from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from dotenv import load_dotenv
import os, json, re, traceback
from collections import deque
from datetime import datetime, timezone
from pathlib import Path

# --- Environment Setup ---
def load_environment_files(base_dir: Path | None = None) -> None:
    service_dir = base_dir or Path(__file__).resolve().parent
    candidates = [
        service_dir / ".env",
        service_dir / ".env.prod",
        service_dir.parent / ".env.prod",
    ]
    for env_file in candidates:
        if env_file.exists():
            load_dotenv(dotenv_path=env_file, override=False)

load_environment_files()

# --- App Initialization ---
app = FastAPI(
    title="MecaManage AI Diagnosis",
    description="Service de diagnostic basé sur RAG (BM25) — retourne un JSON structuré",
    version="2.0.0"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# --- RAG Setup ---
try:
    from rag_engine import RAGEngine
    rag = RAGEngine()
    print(" RAG Engine chargé avec succès.")
except ImportError:
    print(" ERREUR: rag_engine.py est introuvable dans le répertoire.")
    rag = None

# --- Storage ---
TEST_RESULTS_DIR = Path(__file__).parent / "rag_tests"
TEST_RESULTS_DIR.mkdir(exist_ok=True)
diagnostic_history = deque(maxlen=10)

# --- Heuristic keyword tables ---
_URGENCY_CRITICAL = [
    "freins", "frein", "fuite", "sécurité", "airbag", "direction",
    "surcharge", "surchauffe", "incendie", "fumée", "explosion",
    "perte de contrôle", "accident", "blocage", "defaillance critique",
]
_URGENCY_HIGH = [
    "moteur", "batterie vide", "demarrage", "huile", "transmission",
    "boite de vitesse", "embrayage", "turbo", "injection", "alternateur",
]
_URGENCY_MEDIUM = [
    "voyant", "climatisation", "suspension", "amortisseur", "bruit",
    "vibration", "electricite", "capteur", "echappement",
]

_WORKSHOP_MAP = {
    "électricité": ["batterie", "alternateur", "faisceau", "capteur", "voyant", "electricite", "court-circuit"],
    "carrosserie": ["carrosserie", "pare-choc", "rouille", "peinture", "vitre", "portière"],
    "climatisation": ["climatisation", "clim", "ac", "refrigerant", "compresseur"],
    "transmission": ["boite de vitesse", "transmission", "embrayage", "differentiel", "cardan"],
    "mécanique générale": [],  # fallback
}

_COST_MAP = {
    "Critical": "500-2000 EUR",
    "High": "200-800 EUR",
    "Medium": "80-300 EUR",
    "Low": "30-150 EUR",
}


def _normalise(text: str) -> str:
    import unicodedata
    return "".join(
        c for c in unicodedata.normalize("NFD", text.lower())
        if unicodedata.category(c) != "Mn"
    )


def _detect_urgency(symptoms: str, rag_text: str) -> str:
    combined = _normalise(symptoms + " " + rag_text)
    for kw in _URGENCY_CRITICAL:
        if _normalise(kw) in combined:
            return "Critical"
    for kw in _URGENCY_HIGH:
        if _normalise(kw) in combined:
            return "High"
    for kw in _URGENCY_MEDIUM:
        if _normalise(kw) in combined:
            return "Medium"
    return "Low"


def _detect_workshop(symptoms: str, rag_text: str) -> str:
    combined = _normalise(symptoms + " " + rag_text)
    for workshop, keywords in _WORKSHOP_MAP.items():
        for kw in keywords:
            if _normalise(kw) in combined:
                return workshop.capitalize()
    return "Mécanique générale"


def _extract_diagnosis(rag_chunks: list[str], symptoms: str) -> str:
    """Return a concise diagnosis sentence from top RAG chunk + symptoms."""
    if not rag_chunks:
        return f"Analyse des symptômes: {symptoms[:200]}"
    first = rag_chunks[0]
    # Try to grab first meaningful sentence
    sentences = re.split(r"(?<=[.!?])\s+", first.strip())
    top = " ".join(sentences[:2]).strip()
    return top if len(top) > 20 else f"Problème identifié: {symptoms[:150]}"


def _confidence(rag_sources: int, urgency: str) -> float:
    base = min(0.40 + rag_sources * 0.10, 0.85)
    if urgency in ("Critical", "High"):
        return round(min(base + 0.05, 0.90), 2)
    return round(base, 2)


def _recommend_actions(urgency: str, workshop: str, symptoms: str) -> list[str]:
    actions = []
    norm = _normalise(symptoms)
    if "frein" in norm or "freinage" in norm:
        actions.append("Vérifier immédiatement l'état des plaquettes et disques de frein")
    if "huile" in norm or "lubrifiant" in norm:
        actions.append("Contrôler le niveau d'huile moteur et vérifier les fuites")
    if "batterie" in norm or "demarrage" in norm:
        actions.append("Tester la batterie et l'alternateur avec un multimètre")
    if "surchauffe" in norm or "temperature" in norm:
        actions.append("Vérifier le niveau de liquide de refroidissement et le radiateur")
    if "bruit" in norm or "claquement" in norm or "grincement" in norm:
        actions.append("Localiser la source du bruit et inspecter les pièces mobiles concernées")
    if "voyant" in norm:
        actions.append("Effectuer un diagnostic OBD pour lire les codes défauts")

    # Generic actions based on urgency
    if urgency == "Critical":
        actions.insert(0, "⛔ Ne pas utiliser le véhicule avant inspection professionnelle")
    elif urgency == "High":
        actions.insert(0, "Planifier une intervention en urgence sous 48 h")
    else:
        actions.append("Planifier un entretien lors du prochain créneau disponible")

    actions.append(f"Confier le véhicule à un atelier: {workshop}")
    return actions[:5]  # cap at 5


# --- Pydantic Models ---
class DiagnosisRequest(BaseModel):
    symptoms: str
    vehicle_brand: str
    vehicle_model: str
    vehicle_year: int
    mileage: int
    fuel_type: str
    chef_atelier_id: str | None = None
    garage_id: str | None = None


# --- Endpoints ---
@app.get("/health")
def health():
    return {
        "status": "ok",
        "rag_ready": rag is not None,
        "chunks_count": rag.get_stats().get("chunks_count", 0) if rag else 0,
    }


@app.post("/diagnose")
async def diagnose(req: DiagnosisRequest):
    try:
        if not rag:
            raise HTTPException(status_code=500, detail="Moteur RAG non disponible")

        # 1. RAG search
        print(f" Recherche RAG pour : {req.symptoms[:80]}")
        raw = rag.search(req.symptoms, k=5)

        # rag.search returns a joined string; split back into chunks for counting
        chunks: list[str] = [c.strip() for c in re.split(r"\n---\n", raw) if c.strip()]
        rag_sources_used = len(chunks)

        # 2. Heuristic analysis
        combined_rag = " ".join(chunks)
        urgency = _detect_urgency(req.symptoms, combined_rag)
        workshop = _detect_workshop(req.symptoms, combined_rag)
        diagnosis = _extract_diagnosis(chunks, req.symptoms)
        confidence = _confidence(rag_sources_used, urgency)
        actions = _recommend_actions(urgency, workshop, req.symptoms)
        cost_range = _COST_MAP[urgency]

        # 3. Save raw RAG result for traceability
        ts = datetime.now().strftime("%Y%m%d_%H%M%S")
        rag_file = TEST_RESULTS_DIR / f"rag_{ts}.json"
        with open(rag_file, "w", encoding="utf-8") as f:
            json.dump({
                "metadata": {
                    "timestamp": datetime.now(timezone.utc).isoformat(),
                    "vehicle": f"{req.vehicle_brand} {req.vehicle_model} ({req.vehicle_year})",
                    "symptoms": req.symptoms,
                },
                "chunks": chunks,
            }, f, indent=2, ensure_ascii=False)

        # 4. Return structured response matching IAService.cs contract
        return {
            "diagnosis": diagnosis,
            "confidence_score": confidence,
            "recommended_workshop": workshop,
            "urgency_level": urgency,
            "estimated_cost_range": cost_range,
            "recommended_actions": actions,
            "rag_sources_used": rag_sources_used,
        }

    except HTTPException:
        raise
    except Exception as e:
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))
import math
import os
import re
import unicodedata
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path


_FR_AUTO_SYNONYMS: dict[str, list[str]] = {
    "panne":        ["defaut", "defaillance", "probleme", "anomalie"],
    "defaut":       ["panne", "defaillance", "anomalie", "erreur"],
    "voyant":       ["temoin", "lampe", "indicateur", "alerte"],
    "moteur":       ["engine", "bloc", "thermique"],
    "batterie":     ["accumulateur", "accu", "12v"],
    "frein":        ["freinage", "freinages", "plaquette", "disque"],
    "huile":        ["lubrifiant", "vidange", "fluide"],
    "vitesse":      ["rapport", "boite", "transmission"],
    "demarrage":    ["demarrer", "demarreur", "demarrage"],
    "surchauffe":   ["chauffe", "chaud", "temperature", "refroidissement"],
    "bruit":        ["bruit", "claquement", "sifflement", "grincement", "cognement"],
    "fuite":        ["fuite", "suintement", "ecoulement", "perte"],
    "electrique":   ["electricite", "faisceau", "circuit", "tension", "courant"],
    "climatisation":["clim", "clima", "ac", "refrigerant", "compresseur"],
    "direction":    ["volant", "direction", "cremaillere", "servo"],
    "suspension":   ["amortisseur", "ressort", "silence", "rotule"],
    "allumage":     ["bougie", "bobine", "allumeur", "allumage"],
    "injection":    ["injecteur", "injection", "rampe", "carburant", "essence"],
    "embrayage":    ["embrayage", "disque", "butee", "pedale"],
    "echappement":  ["echappement", "pot", "catalyseur", "sonde", "lambda"],
}

# BM25 hyper-parameters
_BM25_K1 = 1.5   # term-frequency saturation
_BM25_B  = 0.75  # length normalisation


class RAGEngine:
    def __init__(self):
        self.rag_file = os.getenv("RAG_FILE_PATH", "./rag/knowledge_base.txt")
        self.chunks: list[str] = []
        self.last_results_count: int = 0
        self.last_loaded_at: str | None = None
        self._document_frequencies: Counter = Counter()
        self._chunk_term_counts: list[Counter] = []   # raw term counts per chunk
        self._chunk_lengths: list[int] = []           # token count per chunk
        self._avg_chunk_length: float = 0.0
        # TF-IDF structures (kept for get_stats compatibility)
        self._chunk_vectors: list[dict] = []
        self._chunk_norms: list[float] = []
        self._load_knowledge_base()

    # ------------------------------------------------------------------
    # Text normalisation helpers
    # ------------------------------------------------------------------

    @staticmethod
    def _remove_accents(text: str) -> str:
        """Convert accented characters to their ASCII base form."""
        return "".join(
            c for c in unicodedata.normalize("NFD", text)
            if unicodedata.category(c) != "Mn"
        )

    def _tokenize(self, text: str) -> list[str]:
        """Lowercase, strip accents, then extract word tokens."""
        normalised = self._remove_accents(text.lower())
        return re.findall(r"\b\w+\b", normalised)

    def _expand_query(self, tokens: list[str]) -> list[str]:
        """Add synonyms for known automotive French terms (no duplicates)."""
        expanded = list(tokens)
        seen = set(tokens)
        for token in tokens:
            for synonym in _FR_AUTO_SYNONYMS.get(token, []):
                if synonym not in seen:
                    expanded.append(synonym)
                    seen.add(synonym)
        return expanded

    # ------------------------------------------------------------------
    # Index building
    # ------------------------------------------------------------------

    def _build_index(self) -> None:
        self._document_frequencies = Counter()
        self._chunk_vectors = []
        self._chunk_norms = []
        self._chunk_term_counts = []
        self._chunk_lengths = []

        tokenized_chunks = [self._tokenize(chunk) for chunk in self.chunks]
        total_docs = len(tokenized_chunks)

        for tokens in tokenized_chunks:
            self._document_frequencies.update(set(tokens))

        total_length = sum(len(t) for t in tokenized_chunks)
        self._avg_chunk_length = total_length / total_docs if total_docs else 0.0

        for tokens in tokenized_chunks:
            term_counts = Counter(tokens)
            self._chunk_term_counts.append(term_counts)
            self._chunk_lengths.append(len(tokens))

            # --- TF-IDF vector (kept for fallback / stats) ---
            if not term_counts:
                self._chunk_vectors.append({})
                self._chunk_norms.append(0.0)
                continue

            total_terms = sum(term_counts.values())
            vector: dict[str, float] = {}
            for token, count in term_counts.items():
                tf = count / total_terms
                idf = math.log((1 + total_docs) / (1 + self._document_frequencies[token])) + 1
                vector[token] = tf * idf

            norm = math.sqrt(sum(w * w for w in vector.values()))
            self._chunk_vectors.append(vector)
            self._chunk_norms.append(norm)

    # ------------------------------------------------------------------
    # Knowledge base loading
    # ------------------------------------------------------------------

    def _load_knowledge_base(self) -> None:
        rag_path = Path(self.rag_file)
        if not rag_path.exists():
            print("knowledge_base.txt introuvable")
            self.chunks = []
            self._build_index()
            self.last_loaded_at = None
            return

        with rag_path.open("r", encoding="utf-8") as f:
            content = f.read()

        raw_chunks = re.split(r"\n-{3,}\n|\n\n", content)
        self.chunks = [c.strip() for c in raw_chunks if len(c.strip()) > 30]
        self._build_index()
        self.last_loaded_at = datetime.now(timezone.utc).isoformat()
        print(f"RAG charge : {len(self.chunks)} chunks")

    # ------------------------------------------------------------------
    # BM25 scoring
    # ------------------------------------------------------------------

    def _bm25_score(self, query_tokens: list[str], chunk_idx: int) -> float:
        """Compute BM25 score for one chunk given expanded query tokens."""
        total_docs = max(len(self.chunks), 1)
        term_counts = self._chunk_term_counts[chunk_idx]
        chunk_len = self._chunk_lengths[chunk_idx]
        avg_len = self._avg_chunk_length or 1.0

        score = 0.0
        for token in query_tokens:
            df = self._document_frequencies.get(token, 0)
            if df == 0:
                continue
            idf = math.log((total_docs - df + 0.5) / (df + 0.5) + 1)
            tf = term_counts.get(token, 0)
            numerator = tf * (_BM25_K1 + 1)
            denominator = tf + _BM25_K1 * (1 - _BM25_B + _BM25_B * chunk_len / avg_len)
            score += idf * (numerator / denominator)
        return score

    # ------------------------------------------------------------------
    # Public search interface
    # ------------------------------------------------------------------

    def search(self, query: str, k: int = 5) -> str:
        if not self.chunks:
            self.last_results_count = 0
            return "Aucune base de connaissances disponible."

        raw_tokens = self._tokenize(query)
        expanded_tokens = self._expand_query(raw_tokens)

        scored: list[tuple[float, str]] = []
        for idx, chunk in enumerate(self.chunks):
            score = self._bm25_score(expanded_tokens, idx)
            scored.append((score, chunk))

        scored.sort(key=lambda item: item[0], reverse=True)

        # Keep chunks with a positive BM25 score; fall back to top-k if none match
        top = [chunk for score, chunk in scored[:k] if score > 0]
        if not top:
            top = [chunk for _, chunk in scored[:k]]

        self.last_results_count = len(top)
        return "\n\n---\n\n".join(top)

    # ------------------------------------------------------------------
    # Lifecycle
    # ------------------------------------------------------------------

    def reload(self) -> None:
        self.chunks = []
        self.last_results_count = 0
        self._load_knowledge_base()

    def get_stats(self) -> dict:
        rag_path = Path(self.rag_file)
        return {
            "rag_file_path": str(rag_path.resolve()),
            "chunks_count": len(self.chunks),
            "last_loaded_at": self.last_loaded_at,
            "last_results_count": self.last_results_count,
            "tfidf_ready": bool(self.chunks and self._chunk_vectors),
        }

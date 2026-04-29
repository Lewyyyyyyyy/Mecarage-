import json
import os
from collections import deque
from pathlib import Path
from types import SimpleNamespace

from fastapi.testclient import TestClient

import main


class FakeRAG:
    def __init__(self):
        self.last_results_count = 2
        self.reload_called = False

    def search(self, query: str, k: int = 5) -> str:
        self.last_results_count = 2
        return "Chunk A\n\n---\n\nChunk B"

    def reload(self):
        self.reload_called = True

    def get_stats(self) -> dict:
        return {
            "rag_file_path": "D:/fake/knowledge_base.txt",
            "chunks_count": 123,
            "last_loaded_at": "2026-03-08T10:00:00+00:00",
            "last_results_count": self.last_results_count,
            "tfidf_ready": True,
        }


class FakeGeminiModels:
    def __init__(self, text: str = None, error: Exception = None, responses=None):
        self._text = text
        self._error = error
        self._responses = list(responses or [])
        self.calls = 0

    def generate_content(self, model: str, contents: str):
        self.calls += 1
        if self._responses:
            current = self._responses.pop(0)
            if isinstance(current, Exception):
                raise current
            return SimpleNamespace(text=current)
        if self._error:
            raise self._error
        return SimpleNamespace(text=self._text)


class FakeGeminiModelsGenerateOnly:
    def __init__(self, text: str):
        self.text = text
        self.calls = 0

    def generate(self, model: str, contents: str):
        self.calls += 1
        return SimpleNamespace(text=self.text)


class FakeGeminiClient:
    def __init__(self, text: str = None, error: Exception = None, responses=None):
        self.models = FakeGeminiModels(text=text, error=error, responses=responses)


DIAGNOSIS_PAYLOAD = {
    "symptoms": "voyant moteur allume avec perte de puissance",
    "vehicle_brand": "Peugeot",
    "vehicle_model": "208",
    "vehicle_year": 2020,
    "mileage": 85000,
    "fuel_type": "essence",
}


def make_client(monkeypatch, rag=None, gemini=None):
    monkeypatch.setattr(main, "rag", rag or FakeRAG())
    monkeypatch.setattr(main, "gemini_client", gemini)
    monkeypatch.setattr(main, "diagnostic_history", deque(maxlen=10))
    return TestClient(main.app)


def test_kb_stats_endpoint_returns_expected_shape(monkeypatch):
    client = make_client(monkeypatch, rag=FakeRAG(), gemini=None)

    response = client.get("/kb-stats")

    assert response.status_code == 200
    data = response.json()
    assert data["chunks_count"] == 123
    assert data["tfidf_ready"] is True
    assert data["rag_file_path"].endswith("knowledge_base.txt")


def test_reload_kb_endpoint_triggers_reload(monkeypatch):
    fake_rag = FakeRAG()
    client = make_client(monkeypatch, rag=fake_rag, gemini=None)

    response = client.post("/reload-kb")

    assert response.status_code == 200
    assert fake_rag.reload_called is True
    assert response.json()["chunks_count"] == 123


def test_diagnose_returns_structured_response(monkeypatch):
    fake_response = {
        "diagnosis": "Probable defaillance bobine ou sonde lambda.",
        "confidence_score": 0.88,
        "recommended_workshop": "mecanique",
        "urgency_level": "modere",
        "estimated_cost_range": "120-350 TND",
        "recommended_actions": ["Lecture OBD2", "Controle allumage"],
    }
    client = make_client(
        monkeypatch,
        rag=FakeRAG(),
        gemini=FakeGeminiClient(text=f"Voici le JSON:\n{json.dumps(fake_response)}"),
    )

    response = client.post("/diagnose", json=DIAGNOSIS_PAYLOAD)

    assert response.status_code == 200
    data = response.json()
    assert data["diagnosis"] == fake_response["diagnosis"]
    assert data["recommended_workshop"] == "mecanique"
    assert data["rag_sources_used"] == 2


def test_diagnose_uses_generate_fallback_when_generate_content_missing(monkeypatch):
    fake_response = {
        "diagnosis": "Probable batterie faible.",
        "confidence_score": 0.77,
        "recommended_workshop": "electrique",
        "urgency_level": "modere",
        "estimated_cost_range": "70-160 TND",
        "recommended_actions": ["Tester batterie", "Verifier alternateur"],
    }

    class GeminiClientWithGenerateOnly:
        def __init__(self, text: str):
            self.models = FakeGeminiModelsGenerateOnly(text)

    client = make_client(
        monkeypatch,
        rag=FakeRAG(),
        gemini=GeminiClientWithGenerateOnly(json.dumps(fake_response)),
    )

    response = client.post("/diagnose", json=DIAGNOSIS_PAYLOAD)

    assert response.status_code == 200
    assert response.json()["diagnosis"] == fake_response["diagnosis"]


def test_load_environment_files_prefers_service_files_over_root(monkeypatch, tmp_path):
    monkeypatch.delenv("GEMINI_API_KEY", raising=False)

    service_dir = tmp_path / "ia-service"
    service_dir.mkdir()
    (service_dir / ".env.prod").write_text("GEMINI_API_KEY=service-key\n", encoding="utf-8")
    (tmp_path / ".env.prod").write_text("GEMINI_API_KEY=root-key\n", encoding="utf-8")

    main.load_environment_files(base_dir=service_dir)

    assert os.environ["GEMINI_API_KEY"] == "service-key"


def test_diagnose_writes_preview_file(monkeypatch, tmp_path):
    preview_file = tmp_path / "diagnosis_preview.json"
    monkeypatch.setattr(main, "PREVIEW_OUTPUT_PATH", preview_file)

    fake_response = {
        "diagnosis": "Suspicion de batterie faible.",
        "confidence_score": 0.81,
        "recommended_workshop": "electrique",
        "urgency_level": "modere",
        "estimated_cost_range": "80-180 TND",
        "recommended_actions": ["Tester la batterie", "Verifier l'alternateur"],
    }
    client = make_client(
        monkeypatch,
        rag=FakeRAG(),
        gemini=FakeGeminiClient(text=json.dumps(fake_response)),
    )

    response = client.post("/diagnose", json=DIAGNOSIS_PAYLOAD)

    assert response.status_code == 200
    assert preview_file.exists()

    preview_data = json.loads(preview_file.read_text(encoding="utf-8"))
    assert preview_data["request"]["symptoms"] == DIAGNOSIS_PAYLOAD["symptoms"]
    assert preview_data["request"]["vehicle_brand"] == DIAGNOSIS_PAYLOAD["vehicle_brand"]
    assert preview_data["result"]["diagnosis"] == fake_response["diagnosis"]
    assert preview_data["result"]["rag_sources_used"] == 2


def test_diagnose_persists_chef_routing_metadata(monkeypatch):
    routed_payload = {
        **DIAGNOSIS_PAYLOAD,
        "chef_atelier_id": "c8b48d9e-8e8a-4f63-8d27-8ebd2e3c7a21",
        "garage_id": "a42e1fc7-82fd-4831-8c5e-5be77ba32054",
    }

    fake_response = {
        "diagnosis": "Suspicion de batterie faible.",
        "confidence_score": 0.81,
        "recommended_workshop": "electrique",
        "urgency_level": "modere",
        "estimated_cost_range": "80-180 TND",
        "recommended_actions": ["Tester la batterie", "Verifier l'alternateur"],
    }
    client = make_client(
        monkeypatch,
        rag=FakeRAG(),
        gemini=FakeGeminiClient(text=json.dumps(fake_response)),
    )

    response = client.post("/diagnose", json=routed_payload)

    assert response.status_code == 200
    assert response.json()["routing"]["chef_atelier_id"] == routed_payload["chef_atelier_id"]

    saved_path = response.json()["file_saved"]
    preview_data = json.loads(Path(saved_path).read_text(encoding="utf-8"))
    assert preview_data["metadata"]["chef_atelier_id"] == routed_payload["chef_atelier_id"]
    assert preview_data["metadata"]["garage_id"] == routed_payload["garage_id"]


def test_diagnose_returns_422_when_gemini_json_is_invalid(monkeypatch):
    client = make_client(monkeypatch, rag=FakeRAG(), gemini=FakeGeminiClient(text="{invalid json}"))

    response = client.post("/diagnose", json=DIAGNOSIS_PAYLOAD)

    assert response.status_code == 422
    assert "JSON error" in response.json()["detail"]


def test_diagnose_returns_500_when_gemini_fails_after_retries(monkeypatch):
    gemini = FakeGeminiClient(error=RuntimeError("quota exceeded"))
    client = make_client(monkeypatch, rag=FakeRAG(), gemini=gemini)

    response = client.post("/diagnose", json=DIAGNOSIS_PAYLOAD)

    assert response.status_code == 500
    assert response.json()["detail"] == "quota exceeded"
    assert gemini.models.calls == 3


def test_diagnose_retries_then_succeeds(monkeypatch):
    fake_response = {
        "diagnosis": "Suspicion bobine allumage.",
        "confidence_score": 0.73,
        "recommended_workshop": "mecanique",
        "urgency_level": "modere",
        "estimated_cost_range": "90-200 TND",
        "recommended_actions": ["Lecture OBD2", "Controle bobines"],
    }
    gemini = FakeGeminiClient(
        responses=[RuntimeError("temporary"), RuntimeError("temporary"), json.dumps(fake_response)]
    )
    client = make_client(monkeypatch, rag=FakeRAG(), gemini=gemini)

    response = client.post("/diagnose", json=DIAGNOSIS_PAYLOAD)

    assert response.status_code == 200
    assert response.json()["diagnosis"] == fake_response["diagnosis"]
    assert gemini.models.calls == 3


def test_diagnose_returns_422_when_confidence_score_is_out_of_range(monkeypatch):
    invalid_response = {
        "diagnosis": "Reponse incoherente.",
        "confidence_score": 1.7,
        "recommended_workshop": "mecanique",
        "urgency_level": "modere",
        "estimated_cost_range": "100-150 TND",
        "recommended_actions": ["Verifier", "Tester"],
    }
    client = make_client(monkeypatch, rag=FakeRAG(), gemini=FakeGeminiClient(text=json.dumps(invalid_response)))

    response = client.post("/diagnose", json=DIAGNOSIS_PAYLOAD)

    assert response.status_code == 422
    assert response.json()["detail"] == "confidence_score must be between 0 and 1"


def test_diagnose_returns_422_when_actions_are_insufficient(monkeypatch):
    invalid_response = {
        "diagnosis": "Suspicion simple.",
        "confidence_score": 0.55,
        "recommended_workshop": "mecanique",
        "urgency_level": "faible",
        "estimated_cost_range": "50-100 TND",
        "recommended_actions": ["Verifier"],
    }
    client = make_client(monkeypatch, rag=FakeRAG(), gemini=FakeGeminiClient(text=json.dumps(invalid_response)))

    response = client.post("/diagnose", json=DIAGNOSIS_PAYLOAD)

    assert response.status_code == 422
    assert response.json()["detail"] == "recommended_actions must contain at least 2 items"


def test_diagnose_returns_422_when_enum_values_are_invalid(monkeypatch):
    invalid_response = {
        "diagnosis": "Suspicion electrique.",
        "confidence_score": 0.61,
        "recommended_workshop": "atelier-magique",
        "urgency_level": "extreme",
        "estimated_cost_range": "80-200 TND",
        "recommended_actions": ["Verifier masse", "Controler batterie"],
    }
    client = make_client(monkeypatch, rag=FakeRAG(), gemini=FakeGeminiClient(text=json.dumps(invalid_response)))

    response = client.post("/diagnose", json=DIAGNOSIS_PAYLOAD)

    assert response.status_code == 422
    assert "Invalid recommended_workshop" in response.json()["detail"]


def test_diagnose_returns_503_when_gemini_is_not_configured(monkeypatch):
    client = make_client(monkeypatch, rag=FakeRAG(), gemini=None)

    response = client.post("/diagnose", json=DIAGNOSIS_PAYLOAD)

    assert response.status_code == 503
    assert response.json()["detail"] == "Gemini client not configured"


def test_diagnostics_history_returns_last_10_newest_first(monkeypatch):
    fake_response = {
        "diagnosis": "Diagnostic stable.",
        "confidence_score": 0.8,
        "recommended_workshop": "mecanique",
        "urgency_level": "modere",
        "estimated_cost_range": "100-250 TND",
        "recommended_actions": ["Action 1", "Action 2"],
    }
    client = make_client(monkeypatch, rag=FakeRAG(), gemini=FakeGeminiClient(text=json.dumps(fake_response)))

    for index in range(12):
        payload = {**DIAGNOSIS_PAYLOAD, "symptoms": f"symptome {index}"}
        response = client.post("/diagnose", json=payload)
        assert response.status_code == 200

    history_response = client.get("/diagnostics-history")

    assert history_response.status_code == 200
    history = history_response.json()
    assert len(history) == 10
    assert history[0]["symptoms"] == "symptome 11"
    assert history[-1]["symptoms"] == "symptome 2"


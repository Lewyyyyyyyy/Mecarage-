from pathlib import Path

from rag_engine import RAGEngine


def write_kb(path: Path, content: str) -> None:
    path.write_text(content.strip() + "\n", encoding="utf-8")


def test_search_prefers_most_relevant_chunk(tmp_path):
    kb_file = tmp_path / "knowledge_base.txt"
    write_kb(
        kb_file,
        """
        BATTERIE - DEMARRAGE DIFFICILE
        Symptomes: demarreur lent batterie faible voyant batterie allume
        Causes possibles: batterie HS alternateur defectueux
        Actions: tester batterie et alternateur

        CLIMATISATION - CLIM NE REFROIDIT PLUS
        Symptomes: air tiede climatisation ne refroidit plus compresseur claque
        Causes possibles: fuite gaz compresseur HS
        Actions: controle fuite et recharge gaz
        """,
    )

    engine = RAGEngine()
    engine.rag_file = str(kb_file)
    engine.reload()

    result = engine.search("demarreur lent voyant batterie allume", k=1)

    assert "BATTERIE - DEMARRAGE DIFFICILE" in result
    assert "CLIMATISATION - CLIM NE REFROIDIT PLUS" not in result
    assert engine.last_results_count == 1


def test_search_returns_fallback_message_when_kb_is_missing(tmp_path):
    engine = RAGEngine()
    engine.rag_file = str(tmp_path / "missing.txt")
    engine.reload()

    result = engine.search("nimporte quelle panne", k=3)

    assert result == "Aucune base de connaissances disponible."
    assert engine.last_results_count == 0
    assert engine.get_stats()["tfidf_ready"] is False


def test_reload_updates_chunk_count_and_loaded_timestamp(tmp_path):
    kb_file = tmp_path / "knowledge_base.txt"
    write_kb(
        kb_file,
        """
        FREIN - BRUIT AU FREINAGE
        Symptomes: grincement freinage
        Causes possibles: plaquettes usees
        Actions: inspection freinage
        """,
    )

    engine = RAGEngine()
    engine.rag_file = str(kb_file)
    engine.reload()
    first_stats = engine.get_stats()

    write_kb(
        kb_file,
        """
        FREIN - BRUIT AU FREINAGE
        Symptomes: grincement freinage
        Causes possibles: plaquettes usees
        Actions: inspection freinage

        MOTEUR - VOYANT MOTEUR ALLUME
        Symptomes: voyant moteur allume perte de puissance
        Causes possibles: sonde lambda bobine allumage
        Actions: lecture OBD2
        """,
    )
    engine.reload()
    second_stats = engine.get_stats()

    assert first_stats["chunks_count"] == 1
    assert second_stats["chunks_count"] == 2
    assert second_stats["last_loaded_at"] is not None


def test_search_with_unknown_terms_falls_back_to_top_k_chunks(tmp_path):
    kb_file = tmp_path / "knowledge_base.txt"
    write_kb(
        kb_file,
        """
        ECHAPPEMENT - BRUIT FORT
        Symptomes: bruit fort echappement odeur gaz
        Causes possibles: silencieux perce
        Actions: inspection visuelle

        SUSPENSION - BRUIT EN VIRAGE
        Symptomes: craquement en virage volant qui tire
        Causes possibles: rotule usee silent bloc use
        Actions: controle train avant
        """,
    )

    engine = RAGEngine()
    engine.rag_file = str(kb_file)
    engine.reload()

    result = engine.search("symptomes extraterrestres inconnus", k=2)

    assert "ECHAPPEMENT - BRUIT FORT" in result
    assert "SUSPENSION - BRUIT EN VIRAGE" in result
    assert engine.last_results_count == 2


# Generates GameLocalizationExtraLookup.cs (Thai, Spanish, Portuguese, French, Italian, German).
import re, glob, os, json, time

ROOT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Scripts", "UI")
OUT = os.path.join(ROOT, "GameLocalizationExtraLookup.cs")
CACHE = os.path.join(os.path.dirname(__file__), "extra_languages_cache.json")

LANGS = [
    ("Th", "th"),
    ("Es", "es"),
    ("Pt", "pt"),
    ("Fr", "fr"),
    ("It", "it"),
    ("De", "de"),
]

def unescape(s):
    out = []
    i = 0
    while i < len(s):
        if s[i] == "\\" and i + 1 < len(s):
            n = s[i + 1]
            if n == "n":
                out.append("\n"); i += 2; continue
            if n == "r":
                out.append("\r"); i += 2; continue
            if n == "t":
                out.append("\t"); i += 2; continue
            if n == '"':
                out.append('"'); i += 2; continue
            if n == "\\":
                out.append("\\"); i += 2; continue
        out.append(s[i])
        i += 1
    return "".join(out)

def extract_pairs():
    en_keys = {}
    pat = re.compile(r'Pick\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"')
    fmt = re.compile(
        r'Format\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*,'
    )
    for f in glob.glob(os.path.join(ROOT, "GameLocalization*.cs")):
        if "Coordinator" in f or "ChineseLookup" in f or "ExtraLookup" in f:
            continue
        text = open(f, encoding="utf-8").read()
        for m in pat.finditer(text):
            en = unescape(m.group(2))
            en_keys.setdefault(en, True)
        for m in fmt.finditer(text):
            en = unescape(m.group(2))
            en_keys.setdefault(en, True)
    return sorted(en_keys.keys(), key=str.lower)

def is_safe_literal_char(c):
    o = ord(c)
    if o < 128:
        return True
    if 0x00C0 <= o <= 0x024F:
        return True
    if 0x1E00 <= o <= 0x1EFF:
        return True
    if 0x0E00 <= o <= 0x0E7F:
        return True
    if 0x4E00 <= o <= 0x9FFF:
        return True
    if 0x3040 <= o <= 0x30FF:
        return True
    if 0xAC00 <= o <= 0xD7AF:
        return True
    if 0x3000 <= o <= 0x303F:
        return True
    if 0xFF00 <= o <= 0xFFEF:
        return True
    return False

def cs_escape(s):
    out = []
    for c in s:
        if c == "\\":
            out.append("\\\\")
        elif c == '"':
            out.append('\\"')
        elif c == "\n":
            out.append("\\n")
        elif c == "\r":
            out.append("\\r")
        elif c == "\t":
            out.append("\\t")
        elif ord(c) < 32 or not is_safe_literal_char(c):
            out.append(f"\\u{ord(c):04x}")
        else:
            out.append(c)
    return "".join(out)

def protect(text):
    mapping = {}
    idx = 0

    def take(match):
        nonlocal idx
        key = f"__PH{idx}__"
        mapping[key] = match.group(0)
        idx += 1
        return key

    protected = re.sub(r"\{[^{}]+\}", take, text)
    protected = re.sub(r"<color=[^>]+>|</color>", take, protected)
    return protected, mapping

def restore(text, mapping):
    for key in sorted(mapping.keys(), key=len, reverse=True):
        text = text.replace(key, mapping[key])
    return text

def manual_overrides():
    # en -> (th, es, pt, fr, it, de)
    def t(th, es, pt, fr, it, de):
        return (th, es, pt, fr, it, de)

    m = {}
    m["Start Game"] = t("เริ่มเกม", "Iniciar partida", "Iniciar jogo", "Commencer", "Avvia partita", "Spiel starten")
    m["Quit Game"] = t("ออกจากเกม", "Salir del juego", "Sair do jogo", "Quitter", "Esci dal gioco", "Spiel beenden")
    m["Settings"] = t("การตั้งค่า", "Ajustes", "Configurações", "Paramètres", "Impostazioni", "Einstellungen")
    m["Mercenary Archive"] = t("คลังทหารรับจ้าง", "Archivo de mercenarios", "Arquivo de mercenários", "Archives des mercenaires", "Archivio mercenari", "Söldner-Archiv")
    m["Paused"] = t("หยุดชั่วคราว", "Pausa", "Pausado", "Pause", "In pausa", "Pausiert")
    m["Continue"] = t("เล่นต่อ", "Continuar", "Continuar", "Continuer", "Continua", "Fortsetzen")
    m["Restart"] = t("เริ่มใหม่", "Reiniciar", "Reiniciar", "Recommencer", "Ricomincia", "Neustart")
    m["Sound"] = t("เสียง", "Sonido", "Som", "Son", "Audio", "Ton")
    m["Master Volume"] = t("ระดับเสียงหลัก", "Volumen principal", "Volume principal", "Volume principal", "Volume principale", "Gesamtlautstärke")
    m["BGM Volume"] = t("ระดับเสียง BGM", "Volumen BGM", "Volume BGM", "Volume BGM", "Volume BGM", "BGM-Lautstärke")
    m["Display"] = t("หน้าจอ / การแสดงผล", "Pantalla", "Tela", "Affichage", "Schermo", "Anzeige")
    m["Fullscreen"] = t("เต็มหน้าจอ", "Pantalla completa", "Tela cheia", "Plein écran", "Schermo intero", "Vollbild")
    m["Language"] = t("ภาษา", "Idioma", "Idioma", "Langue", "Lingua", "Sprache")
    m["Party Bonus Alerts"] = t("แจ้งเตือนโบนัสปาร์ตี้", "Alertas de bonificación de grupo", "Alertas de bônus de grupo", "Alertes bonus de groupe", "Avvisi bonus gruppo", "Gruppen-Bonus-Hinweise")
    m["Experience"] = t("ประสบการณ์", "Experiencia", "Experiência", "Expérience", "Esperienza", "Erfahrung")
    m["Refresh"] = t("รีเฟรช", "Actualizar", "Atualizar", "Actualiser", "Aggiorna", "Aktualisieren")
    m["Special Skills"] = t("สกิลพิเศษ", "Habilidades especiales", "Habilidades especiais", "Compétences spéciales", "Abilità speciali", "Spezialfähigkeiten")
    m["Used"] = t("ใช้แล้ว", "Usado", "Usado", "Utilisé", "Usato", "Verwendet")
    m["Normal Combat"] = t("การต่อสู้ปกติ", "Combate normal", "Combate normal", "Combat normal", "Combattimento normale", "Normaler Kampf")
    m["Break Time"] = t("ช่วงพัก", "Descanso", "Intervalo", "Pause", "Pausa", "Pause")
    m["Boss Appears!"] = t("บอสปรากฏ!", "¡Aparece el jefe!", "O chefe aparece!", "Le boss apparaît !", "Compare il boss!", "Boss erscheint!")
    m["Deploy"] = t("วาง", "Desplegar", "Posicionar", "Déployer", "Schiera", "Platzieren")
    m["Shop"] = t("ร้านค้า", "Tienda", "Loja", "Boutique", "Negozio", "Shop")
    m["Evolve"] = t("วิวัฒนาการ", "Evolucionar", "Evoluir", "Évoluer", "Evolvere", "Entwickeln")
    m["None"] = t("ไม่มี", "Ninguno", "Nenhum", "Aucun", "Nessuno", "Keine")
    m["Close"] = t("ปิด", "Cerrar", "Fechar", "Fermer", "Chiudi", "Schließen")
    m["All"] = t("ทั้งหมด", "Todo", "Tudo", "Tout", "Tutto", "Alle")
    m["Try Again"] = t("ลองอีกครั้ง", "Reintentar", "Tentar novamente", "Réessayer", "Riprova", "Erneut versuchen")
    m["Play Again"] = t("เล่นอีกครั้ง", "Jugar de nuevo", "Jogar novamente", "Rejouer", "Gioca di nuovo", "Nochmal spielen")
    m["Battle Result"] = t("ผลการต่อสู้", "Resultado de batalla", "Resultado da batalha", "Résultat du combat", "Risultato battaglia", "Kampfergebnis")
    m["Demo Clear!"] = t("เคลียร์เดโม!", "¡Demo completada!", "Demo concluída!", "Démo terminée !", "Demo completata!", "Demo geschafft!")
    m["Your wall has fallen."] = t("กำแพงของคุณพังทลาย", "Tu muro ha caído.", "Seu muro caiu.", "Votre mur est tombé.", "Il tuo muro è caduto.", "Deine Mauer ist gefallen.")
    m["Round 1 Prep"] = t("เตรียมรอบ 1", "Preparación ronda 1", "Preparação rodada 1", "Préparation manche 1", "Preparazione round 1", "Runde 1 Vorbereitung")
    m["Deploy Units · Use Shop"] = t("วางยูนิต · ใช้ร้านค้า", "Desplegar unidades · Usar tienda", "Posicionar unidades · Usar loja", "Déployer · Boutique", "Schiera unità · Negozio", "Einheiten platzieren · Shop")
    m["Start Round 1  ▶"] = t("เริ่มรอบ 1  ▶", "Iniciar ronda 1  ▶", "Iniciar rodada 1  ▶", "Lancer manche 1  ▶", "Inizia round 1  ▶", "Runde 1 starten  ▶")
    m["Next Round  ▶"] = t("รอบถัดไป  ▶", "Siguiente ronda  ▶", "Próxima rodada  ▶", "Manche suivante  ▶", "Prossimo round  ▶", "Nächste Runde  ▶")
    m["Notice"] = t("แจ้งเตือน", "Aviso", "Aviso", "Notice", "Avviso", "Hinweis")
    m["Acquired!"] = t("ได้รับ!", "¡Obtenido!", "Adquirido!", "Obtenu !", "Ottenuto!", "Erhalten!")
    m["OPTIONS"] = t("OPTIONS", "OPTIONS", "OPTIONS", "OPTIONS", "OPTIONS", "OPTIONS")
    m["ROUND {0}"] = t("ROUND {0}", "RONDA {0}", "RODADA {0}", "MANCHE {0}", "ROUND {0}", "RUNDE {0}")
    m["NEW"] = t("NEW", "NUEVO", "NOVO", "NOUVEAU", "NUOVO", "NEU")
    m["—"] = t("—", "—", "—", "—", "—", "—")
    m["N/A"] = t("N/A", "N/D", "N/D", "N/D", "N/D", "k. A.")
    m["Reroll Used"] = t("ใช้สุ่มใหม่แล้ว", "Repetición usada", "Reroll usado", "Relance utilisée", "Rilancio usato", "Neuwahl verbraucht")
    m["▶ Evolution trait unlock!"] = t("▶ ปลดล็อกคุณสมบัติวิวัฒนาการ!", "▶ ¡Rasgo evolutivo desbloqueado!", "▶ Traço evolutivo desbloqueado!", "▶ Trait d'évolution débloqué !", "▶ Tratto evoluzione sbloccato!", "▶ Evolutions-Trait freigeschaltet!")
    return m

def load_cache():
    if os.path.isfile(CACHE):
        return json.load(open(CACHE, encoding="utf-8"))
    return {}

def save_cache(cache):
    json.dump(cache, open(CACHE, "w", encoding="utf-8"), ensure_ascii=False, indent=2)

def translate_all(keys, manual):
    from deep_translator import GoogleTranslator

    cache = load_cache()
    for en, vals in manual.items():
        cache[en] = {"Th": vals[0], "Es": vals[1], "Pt": vals[2], "Fr": vals[3], "It": vals[4], "De": vals[5]}

    pending = [en for en in keys if en not in cache or len(cache.get(en, {})) < 6]
    print(f"Translating {len(pending)} strings...", flush=True)

    for lang_key, target in LANGS:
        todo = [en for en in pending if lang_key not in cache.get(en, {})]
        if not todo:
            print(f"  {target}: cached", flush=True)
            continue
        translator = GoogleTranslator(source="en", target=target)
        batch_size = 20
        for start in range(0, len(todo), batch_size):
            chunk = todo[start : start + batch_size]
            payloads, maps = [], []
            for en in chunk:
                protected, mapping = protect(en)
                payloads.append(protected)
                maps.append(mapping)
            translated = None
            for attempt in range(3):
                try:
                    translated = translator.translate_batch(payloads)
                    break
                except Exception as ex:
                    print(f"  {target} retry {attempt + 1}: {ex}", flush=True)
                    time.sleep(1.5 * (attempt + 1))
            if translated is None:
                translated = []
                for payload in payloads:
                    time.sleep(0.2)
                    try:
                        translated.append(translator.translate(payload))
                    except Exception:
                        translated.append(payload)
            for en, tr, mapping in zip(chunk, translated, maps):
                cache.setdefault(en, {})[lang_key] = restore(tr or en, mapping)
            save_cache(cache)
            print(f"  {target}: {min(start + batch_size, len(todo))}/{len(todo)}", flush=True)
            time.sleep(0.35)

    result = {}
    for en in keys:
        entry = cache.get(en, {})
        result[en] = {
            "Th": entry.get("Th", en),
            "Es": entry.get("Es", en),
            "Pt": entry.get("Pt", en),
            "Fr": entry.get("Fr", en),
            "It": entry.get("It", en),
            "De": entry.get("De", en),
        }
    return result

def write_cs(translations):
    lines = []
    lines.append("using System.Collections.Generic;")
    lines.append("")
    lines.append("/// <summary>English 키 → 태국어/스페인어/포르투갈어/프랑스어/이탈리아어/독일어 조회.</summary>")
    lines.append("public static class GameLocalizationExtraLookup")
    lines.append("{")
    lines.append("    struct Entry { public string Th, Es, Pt, Fr, It, De; }")
    lines.append("    static readonly Dictionary<string, Entry> Map = new Dictionary<string, Entry>();")
    lines.append("")
    lines.append("    static GameLocalizationExtraLookup()")
    lines.append("    {")
    for en in sorted(translations.keys(), key=str.lower):
        e = translations[en]
        lines.append(
            f'        Map["{cs_escape(en)}"] = new Entry {{ '
            f'Th = "{cs_escape(e["Th"])}", Es = "{cs_escape(e["Es"])}", '
            f'Pt = "{cs_escape(e["Pt"])}", Fr = "{cs_escape(e["Fr"])}", '
            f'It = "{cs_escape(e["It"])}", De = "{cs_escape(e["De"])}" }};'
        )
    lines.append("    }")
    lines.append("")
    lines.append("    public static string Get(string en, GameLanguage language)")
    lines.append("    {")
    lines.append("        if (string.IsNullOrEmpty(en)) return en;")
    lines.append("        if (!Map.TryGetValue(en, out Entry e)) return en;")
    lines.append("        switch (language)")
    lines.append("        {")
    lines.append("            case GameLanguage.Thai: return e.Th;")
    lines.append("            case GameLanguage.Spanish: return e.Es;")
    lines.append("            case GameLanguage.Portuguese: return e.Pt;")
    lines.append("            case GameLanguage.French: return e.Fr;")
    lines.append("            case GameLanguage.Italian: return e.It;")
    lines.append("            case GameLanguage.German: return e.De;")
    lines.append("            default: return en;")
    lines.append("        }")
    lines.append("    }")
    lines.append("")
    lines.append("    public static bool MatchesEnglishKey(string enKey, string candidate)")
    lines.append("    {")
    lines.append("        if (string.IsNullOrEmpty(candidate)) return false;")
    lines.append("        if (candidate == enKey) return true;")
    lines.append("        if (!Map.TryGetValue(enKey, out Entry e)) return false;")
    lines.append("        return candidate == e.Th || candidate == e.Es || candidate == e.Pt")
    lines.append("            || candidate == e.Fr || candidate == e.It || candidate == e.De;")
    lines.append("    }")
    lines.append("}")
    open(OUT, "w", encoding="utf-8").write("\n".join(lines) + "\n")
    print(f"Wrote {len(translations)} entries -> {OUT}")

def main():
    keys = extract_pairs()
    manual = manual_overrides()
    translations = translate_all(keys, manual)
    write_cs(translations)

if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""
zombies.csv / rounds.csv 밸런스 데이터 검증 스크립트.

CI(GitHub Actions)에서 Unity 빌드를 시작하기 전에 실행되어,
잘못된 밸런스 데이터로 인해 빌드 후 런타임에서야 발견되는 문제
(예: 존재하지 않는 UID를 라운드가 참조, 음수 스탯 등)를 사전에 차단한다.

실패 시 exit code 1 로 종료하며 GitHub Actions 워크플로가 여기서 중단된다.

사용법:
    python3 tools/validate_csv.py
    python3 tools/validate_csv.py --zombies zombies.csv --rounds rounds.csv
"""

import argparse
import csv
import sys
from pathlib import Path

NUMERIC_FIELDS = ["Hp", "Speed", "damage", "attackCooldown", "attackRange"]
SPAWN_GROUP_COUNT = 7  # Spawn_1 .. Spawn_7


def fail(errors):
    print(f"[validate_csv] {len(errors)}개의 오류가 발견되었습니다:\n", file=sys.stderr)
    for e in errors:
        print(f"  - {e}", file=sys.stderr)
    sys.exit(1)


def load_zombies(path):
    errors = []
    uids = {}
    if not path.exists():
        return {}, [f"{path} 파일을 찾을 수 없습니다."]

    with path.open(encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        missing_cols = [c for c in ["UID"] + NUMERIC_FIELDS if c not in (reader.fieldnames or [])]
        if missing_cols:
            return {}, [f"zombies.csv에 필수 컬럼이 없습니다: {missing_cols}"]

        for row_num, row in enumerate(reader, start=2):  # header is row 1
            uid = (row.get("UID") or "").strip()
            if not uid:
                errors.append(f"zombies.csv 행 {row_num}: UID가 비어 있습니다.")
                continue
            if uid in uids:
                errors.append(f"zombies.csv 행 {row_num}: UID '{uid}' 가 행 {uids[uid]}과 중복됩니다.")
            else:
                uids[uid] = row_num

            for field in NUMERIC_FIELDS:
                raw = (row.get(field) or "").strip()
                try:
                    value = float(raw)
                except ValueError:
                    errors.append(f"zombies.csv 행 {row_num} ({uid}): {field} 값 '{raw}' 이(가) 숫자가 아닙니다.")
                    continue
                if value < 0:
                    errors.append(f"zombies.csv 행 {row_num} ({uid}): {field} 값이 음수입니다 ({value}).")
                if field == "Hp" and value == 0:
                    errors.append(f"zombies.csv 행 {row_num} ({uid}): Hp가 0입니다.")

    return uids, errors


def load_rounds(path, known_uids):
    errors = []
    if not path.exists():
        return [f"{path} 파일을 찾을 수 없습니다."]

    with path.open(encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        header_len = len(reader.fieldnames or [])
        if "round" not in (reader.fieldnames or []):
            return ["rounds.csv에 'round' 컬럼이 없습니다."]

        expected_round = 1
        for row_num, row in enumerate(reader, start=2):
            # DictReader silently dumps any columns beyond the header count into
            # row[None]. A non-empty entry there means this row has MORE fields
            # than the header defines (e.g. an extra unlabeled Spawn group) —
            # that data is being silently lost/misaligned at read time.
            overflow = row.get(None)
            if overflow and any(v not in (None, "") for v in overflow):
                errors.append(
                    f"rounds.csv 행 {row_num} (round {row.get('round')}): 헤더({header_len}개 컬럼)보다 "
                    f"필드가 많습니다. 초과 값: {[v for v in overflow if v not in (None, '')]} "
                    f"— Spawn 그룹이 8개 이상 입력되었을 가능성이 있습니다."
                )

            raw_round = (row.get("round") or "").strip()
            try:
                round_num = int(raw_round)
            except ValueError:
                errors.append(f"rounds.csv 행 {row_num}: round 값 '{raw_round}' 이(가) 정수가 아닙니다.")
                round_num = None

            if round_num is not None and round_num != expected_round:
                errors.append(
                    f"rounds.csv 행 {row_num}: round 값이 {round_num}이지만 "
                    f"{expected_round}이어야 합니다 (라운드는 1부터 연속이어야 함)."
                )
            expected_round += 1

            for i in range(1, SPAWN_GROUP_COUNT + 1):
                uid_col, time_col, min_col, max_col = (
                    f"Spawn_{i}", f"Spawn_{i}_time", f"Spawn_{i}_min", f"Spawn_{i}_max"
                )
                uid = (row.get(uid_col) or "").strip()
                if not uid:
                    continue  # 빈 스폰 슬롯은 정상

                if uid not in known_uids:
                    errors.append(
                        f"rounds.csv 행 {row_num} (round {raw_round}): {uid_col}='{uid}' 가 "
                        f"zombies.csv에 존재하지 않는 UID입니다."
                    )

                min_raw = (row.get(min_col) or "").strip()
                max_raw = (row.get(max_col) or "").strip()
                try:
                    min_v, max_v = int(min_raw), int(max_raw)
                    if min_v < 0 or max_v < 0:
                        errors.append(
                            f"rounds.csv 행 {row_num} ({uid_col}): min/max가 음수입니다 "
                            f"(min={min_v}, max={max_v})."
                        )
                    elif min_v > max_v:
                        errors.append(
                            f"rounds.csv 행 {row_num} ({uid_col}): min({min_v}) > max({max_v}) 입니다."
                        )
                except ValueError:
                    errors.append(
                        f"rounds.csv 행 {row_num} ({uid_col}): min/max 값이 숫자가 아닙니다 "
                        f"(min='{min_raw}', max='{max_raw}')."
                    )

    return errors


def main():
    parser = argparse.ArgumentParser(description="zombies.csv / rounds.csv 밸런스 데이터 검증")
    parser.add_argument("--zombies", default="zombies.csv")
    parser.add_argument("--rounds", default="rounds.csv")
    args = parser.parse_args()

    zombies_path = Path(args.zombies)
    rounds_path = Path(args.rounds)

    known_uids, zombie_errors = load_zombies(zombies_path)
    if zombie_errors:
        fail(zombie_errors)

    round_errors = load_rounds(rounds_path, set(known_uids.keys()))
    if round_errors:
        fail(round_errors)

    print(f"[validate_csv] OK — zombies.csv: {len(known_uids)}종, rounds.csv: 검증 통과.")
    sys.exit(0)


if __name__ == "__main__":
    main()

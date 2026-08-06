using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// CSV 파일을 읽고 파싱하는 유틸리티 클래스
/// </summary>
public static class CSVReader
{
    /// <summary>
    /// CSV 파일을 읽어서 딕셔너리 리스트로 반환합니다
    /// 첫 번째 행은 헤더로 사용됩니다
    /// </summary>
    public static List<Dictionary<string, string>> ReadCSV(string filePath)
    {
        List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();
        
        if (!File.Exists(filePath))
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: {filePath}");
            return data;
        }
        
        try
        {
            return ReadCSVFromText(File.ReadAllText(filePath), filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CSV 파일 읽기 오류: {e.Message}");
        }

        return data;
    }

    /// <summary>
    /// 이미 메모리에 올라온 CSV 문자열을 파싱합니다.
    ///
    /// 안드로이드/iOS 빌드에서는 StreamingAssets 폴더가 APK 안에 압축되어 들어가기 때문에
    /// System.IO(File.Exists / File.ReadAllText)로는 절대 읽을 수 없다.
    /// 그런 플랫폼에서는 Resources.Load&lt;TextAsset&gt;으로 읽은 문자열을
    /// 이 메서드에 넘겨서 동일하게 파싱한다.
    /// </summary>
    /// <param name="content">CSV 전체 텍스트</param>
    /// <param name="sourceLabel">로그에 표시할 출처 이름(디버그용)</param>
    public static List<Dictionary<string, string>> ReadCSVFromText(string content, string sourceLabel = "(text)")
    {
        List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();

        if (string.IsNullOrEmpty(content))
        {
            Debug.LogWarning($"CSV 내용이 비어 있습니다: {sourceLabel}");
            return data;
        }

        try
        {
            // 윈도우(\r\n) / 유닉스(\n) 줄바꿈을 모두 처리한다.
            string[] rawLines = content.Split('\n');

            if (rawLines.Length < 2)
            {
                Debug.LogWarning($"CSV에 데이터가 충분하지 않습니다: {sourceLabel}");
                return data;
            }

            // 첫 번째 행을 헤더로 사용
            string[] headers = ParseCSVLine(rawLines[0].TrimEnd('\r'));

            // 나머지 행들을 데이터로 파싱
            for (int i = 1; i < rawLines.Length; i++)
            {
                string line = rawLines[i].TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line)) continue; // 빈 행 건너뛰기

                string[] values = ParseCSVLine(line);
                Dictionary<string, string> row = new Dictionary<string, string>();

                for (int j = 0; j < headers.Length && j < values.Length; j++)
                {
                    row[headers[j].Trim()] = values[j].Trim();
                }

                data.Add(row);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CSV 파싱 오류({sourceLabel}): {e.Message}");
        }

        return data;
    }

    /// <summary>
    /// Resources 폴더에서 CSV TextAsset을 읽어 파싱합니다.
    /// (Assets/Resources/&lt;resourceName&gt;.csv → resourceName은 확장자 없이 전달)
    /// 에디터·PC·안드로이드 등 모든 플랫폼에서 동일하게 동작하므로
    /// 모바일 빌드에서의 기본 경로로 사용한다.
    /// </summary>
    public static List<Dictionary<string, string>> ReadCSVFromResources(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName))
        {
            return new List<Dictionary<string, string>>();
        }

        TextAsset asset = Resources.Load<TextAsset>(resourceName);
        if (asset == null)
        {
            Debug.LogWarning($"Resources에서 CSV를 찾을 수 없습니다: Resources/{resourceName}");
            return new List<Dictionary<string, string>>();
        }

        return ReadCSVFromText(asset.text, $"Resources/{resourceName}");
    }

    /// <summary>
    /// CSV 라인을 파싱합니다 (쉼표로 구분, 따옴표 처리)
    /// </summary>
    static string[] ParseCSVLine(string line)
    {
        List<string> fields = new List<string>();
        bool inQuotes = false;
        string currentField = "";
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // 이스케이프된 따옴표
                    currentField += '"';
                    i++; // 다음 따옴표 건너뛰기
                }
                else
                {
                    // 따옴표 시작/끝
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // 필드 구분자
                fields.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }
        
        // 마지막 필드 추가
        fields.Add(currentField);
        
        return fields.ToArray();
    }
}


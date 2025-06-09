using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using System.Linq;

namespace LJH.BT
{
    /// <summary>
    /// 에이전트 시뮬레이션 결과를 CSV 파일로 내보내는 클래스
    /// 실시간 스트리밍 방식으로 데이터를 추가하며, 파일 관리 기능 제공
    /// </summary>
    public class CSVExporter
    {
        [Header("파일 설정")]
        private string baseDirectory;
        private string currentFilePath;
        private string currentFileName;
        private bool isInitialized = false;
        
        [Header("저장 설정")]
        private bool autoFlush = true;
        private int bufferSize = 50;
        private List<AgentSimulationRecord> recordBuffer = new List<AgentSimulationRecord>();
        
        [Header("파일 관리")]
        private int maxFilesPerDirectory = 100;
        private long maxFileSizeMB = 10;
        private bool enableAutoCleanup = true;
        
        // 이벤트
        public System.Action<string> OnFileCreated;
        public System.Action<string> OnRecordAdded;
        public System.Action<string> OnFileSaved;
        public System.Action<string> OnError;
        
        /// <summary>
        /// CSV Exporter 초기화
        /// </summary>
        /// <param name="baseDir">기본 디렉토리 (기본값: Assets/BTData/SimulationResults/)</param>
        /// <param name="autoFlushEnabled">자동 플러시 활성화</param>
        /// <param name="bufferSize">버퍼 크기</param>
        public void Initialize(string baseDir = null, bool autoFlushEnabled = true, int bufferSize = 50)
        {
            // 기본 디렉토리 설정
            if (string.IsNullOrEmpty(baseDir))
            {
                baseDirectory = Path.Combine(Application.dataPath, "BTData", "SimulationResults");
            }
            else
            {
                baseDirectory = baseDir;
            }
            
            this.autoFlush = autoFlushEnabled;
            this.bufferSize = bufferSize;
            
            // 디렉토리 생성
            CreateDirectoryIfNotExists();
            
            // 새 CSV 파일 생성
            CreateNewCSVFile();
            
            isInitialized = true;
            
            Debug.Log($"[CSVExporter] 초기화 완료: {currentFilePath}");
            OnFileCreated?.Invoke(currentFilePath);
        }
        
        /// <summary>
        /// 디렉토리 생성
        /// </summary>
        private void CreateDirectoryIfNotExists()
        {
            try
            {
                if (!Directory.Exists(baseDirectory))
                {
                    Directory.CreateDirectory(baseDirectory);
                    Debug.Log($"[CSVExporter] 디렉토리 생성: {baseDirectory}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CSVExporter] 디렉토리 생성 실패: {ex.Message}");
                OnError?.Invoke($"디렉토리 생성 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 새 CSV 파일 생성
        /// </summary>
        private void CreateNewCSVFile()
        {
            try
            {
                // 파일명 생성 (타임스탬프 포함)
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                currentFileName = $"SimulationResults_{timestamp}.csv";
                currentFilePath = Path.Combine(baseDirectory, currentFileName);
                
                // CSV 헤더 작성
                using (var writer = new StreamWriter(currentFilePath, false, Encoding.UTF8))
                {
                    writer.WriteLine(AgentSimulationRecord.GetCSVHeader());
                }
                
                Debug.Log($"[CSVExporter] 새 CSV 파일 생성: {currentFileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CSVExporter] CSV 파일 생성 실패: {ex.Message}");
                OnError?.Invoke($"CSV 파일 생성 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 레코드 추가 (버퍼 사용)
        /// </summary>
        /// <param name="record">추가할 레코드</param>
        public void AddRecord(AgentSimulationRecord record)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[CSVExporter] 초기화되지 않음. Initialize()를 먼저 호출하세요.");
                return;
            }
            
            // 레코드 유효성 검증
            if (!record.IsValid())
            {
                Debug.LogWarning($"[CSVExporter] 유효하지 않은 레코드: {record}");
                return;
            }
            
            // 버퍼에 추가
            recordBuffer.Add(record);
            
            Debug.Log($"[CSVExporter] 레코드 추가: {record.agentName} (버퍼: {recordBuffer.Count}/{bufferSize})");
            OnRecordAdded?.Invoke(record.agentName);
            
            // 자동 플러시 체크
            if (autoFlush && recordBuffer.Count >= bufferSize)
            {
                FlushBuffer();
            }
        }
        
        /// <summary>
        /// 여러 레코드 일괄 추가
        /// </summary>
        /// <param name="records">추가할 레코드 목록</param>
        public void AddRecords(IEnumerable<AgentSimulationRecord> records)
        {
            foreach (var record in records)
            {
                AddRecord(record);
            }
        }
        
        /// <summary>
        /// 버퍼의 레코드들을 파일에 기록
        /// </summary>
        public void FlushBuffer()
        {
            if (!isInitialized || recordBuffer.Count == 0)
                return;
            
            try
            {
                // 파일 크기 체크
                CheckFileSize();
                
                // CSV 파일에 추가 모드로 기록
                using (var writer = new StreamWriter(currentFilePath, true, Encoding.UTF8))
                {
                    foreach (var record in recordBuffer)
                    {
                        writer.WriteLine(record.ToCSVString());
                    }
                }
                
                Debug.Log($"[CSVExporter] {recordBuffer.Count}개 레코드를 파일에 저장: {currentFileName}");
                OnFileSaved?.Invoke(currentFilePath);
                
                // 버퍼 클리어
                recordBuffer.Clear();
                
                // 자동 정리
                if (enableAutoCleanup)
                {
                    PerformAutoCleanup();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CSVExporter] 버퍼 플러시 실패: {ex.Message}");
                OnError?.Invoke($"버퍼 플러시 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 파일 크기 체크 및 필요시 새 파일 생성
        /// </summary>
        private void CheckFileSize()
        {
            try
            {
                if (File.Exists(currentFilePath))
                {
                    var fileInfo = new FileInfo(currentFilePath);
                    long fileSizeMB = fileInfo.Length / (1024 * 1024);
                    
                    if (fileSizeMB >= maxFileSizeMB)
                    {
                        Debug.Log($"[CSVExporter] 파일 크기 한계 도달 ({fileSizeMB}MB). 새 파일 생성.");
                        CreateNewCSVFile();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CSVExporter] 파일 크기 체크 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 자동 정리 수행
        /// </summary>
        private void PerformAutoCleanup()
        {
            try
            {
                var csvFiles = Directory.GetFiles(baseDirectory, "SimulationResults_*.csv")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToArray();
                
                // 최대 파일 수 체크
                if (csvFiles.Length > maxFilesPerDirectory)
                {
                    var filesToDelete = csvFiles.Skip(maxFilesPerDirectory);
                    foreach (var file in filesToDelete)
                    {
                        File.Delete(file);
                        Debug.Log($"[CSVExporter] 오래된 파일 삭제: {Path.GetFileName(file)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CSVExporter] 자동 정리 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 즉시 저장 (버퍼 무시)
        /// </summary>
        /// <param name="record">즉시 저장할 레코드</param>
        public void SaveImmediately(AgentSimulationRecord record)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[CSVExporter] 초기화되지 않음.");
                return;
            }
            
            if (!record.IsValid())
            {
                Debug.LogWarning($"[CSVExporter] 유효하지 않은 레코드: {record}");
                return;
            }
            
            try
            {
                CheckFileSize();
                
                using (var writer = new StreamWriter(currentFilePath, true, Encoding.UTF8))
                {
                    writer.WriteLine(record.ToCSVString());
                }
                
                Debug.Log($"[CSVExporter] 즉시 저장 완료: {record.agentName}");
                OnFileSaved?.Invoke(currentFilePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CSVExporter] 즉시 저장 실패: {ex.Message}");
                OnError?.Invoke($"즉시 저장 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 파일 종료 처리
        /// </summary>
        public void Finalize()
        {
            if (!isInitialized)
                return;
            
            // 남은 버퍼 플러시
            if (recordBuffer.Count > 0)
            {
                FlushBuffer();
            }
            
            Debug.Log($"[CSVExporter] 파일 종료: {currentFileName}");
        }
        
        /// <summary>
        /// 현재 파일 정보 반환
        /// </summary>
        /// <returns>파일 정보</returns>
        public FileInfo GetCurrentFileInfo()
        {
            if (!isInitialized || !File.Exists(currentFilePath))
                return null;
            
            return new FileInfo(currentFilePath);
        }
        
        /// <summary>
        /// 저장된 CSV 파일 목록 반환
        /// </summary>
        /// <returns>CSV 파일 경로 목록</returns>
        public List<string> GetSavedCSVFiles()
        {
            if (!Directory.Exists(baseDirectory))
                return new List<string>();
            
            return Directory.GetFiles(baseDirectory, "SimulationResults_*.csv")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();
        }
        
        /// <summary>
        /// CSV 파일 읽기 (분석용)
        /// </summary>
        /// <param name="filePath">파일 경로</param>
        /// <returns>레코드 목록</returns>
        public List<AgentSimulationRecord> ReadCSVFile(string filePath)
        {
            var records = new List<AgentSimulationRecord>();
            
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[CSVExporter] 파일이 존재하지 않음: {filePath}");
                    return records;
                }
                
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                
                // 헤더 건너뛰기
                for (int i = 1; i < lines.Length; i++)
                {
                    var values = lines[i].Split(',');
                    if (values.Length >= 17) // 최소 필드 수 체크
                    {
                        // CSV에서 레코드 파싱 (간단한 버전)
                        // 실제 구현시에는 더 정교한 파싱 필요
                        Debug.Log($"[CSVExporter] CSV 파싱: {values[2]} - {values[3]}");
                    }
                }
                
                Debug.Log($"[CSVExporter] CSV 파일 읽기 완료: {records.Count}개 레코드");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CSVExporter] CSV 파일 읽기 실패: {ex.Message}");
                OnError?.Invoke($"CSV 파일 읽기 실패: {ex.Message}");
            }
            
            return records;
        }
        
        /// <summary>
        /// 통계 정보 반환
        /// </summary>
        /// <returns>통계 정보</returns>
        public CSVExporterStats GetStats()
        {
            var stats = new CSVExporterStats
            {
                currentFileName = currentFileName,
                currentFilePath = currentFilePath,
                bufferCount = recordBuffer.Count,
                bufferSize = bufferSize,
                isInitialized = isInitialized,
                autoFlush = autoFlush,
                totalCSVFiles = GetSavedCSVFiles().Count
            };
            
            if (File.Exists(currentFilePath))
            {
                var fileInfo = new FileInfo(currentFilePath);
                stats.currentFileSizeKB = fileInfo.Length / 1024;
            }
            
            return stats;
        }
        
        /// <summary>
        /// 설정 변경
        /// </summary>
        /// <param name="autoFlushEnabled">자동 플러시</param>
        /// <param name="bufferSize">버퍼 크기</param>
        /// <param name="maxFileSizeMB">최대 파일 크기</param>
        public void UpdateSettings(bool? autoFlushEnabled = null, int? bufferSize = null, long? maxFileSizeMB = null)
        {
            if (autoFlushEnabled.HasValue)
                this.autoFlush = autoFlushEnabled.Value;
            
            if (bufferSize.HasValue && bufferSize.Value > 0)
                this.bufferSize = bufferSize.Value;
            
            if (maxFileSizeMB.HasValue && maxFileSizeMB.Value > 0)
                this.maxFileSizeMB = maxFileSizeMB.Value;
            
            Debug.Log($"[CSVExporter] 설정 업데이트: AutoFlush={this.autoFlush}, BufferSize={this.bufferSize}, MaxSize={this.maxFileSizeMB}MB");
        }
    }
    
    /// <summary>
    /// CSV Exporter 통계 정보
    /// </summary>
    [System.Serializable]
    public struct CSVExporterStats
    {
        public string currentFileName;
        public string currentFilePath;
        public int bufferCount;
        public int bufferSize;
        public long currentFileSizeKB;
        public bool isInitialized;
        public bool autoFlush;
        public int totalCSVFiles;
        
        public override string ToString()
        {
            return $"CSVExporter Stats:\n" +
                   $"파일: {currentFileName}\n" +
                   $"버퍼: {bufferCount}/{bufferSize}\n" +
                   $"크기: {currentFileSizeKB}KB\n" +
                   $"총 파일: {totalCSVFiles}개\n" +
                   $"자동플러시: {autoFlush}";
        }
    }
}

using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ParkSeyang
{
    /// <summary>
    /// 유저의 데이터를 로컬 환경에 JSON 형태로 저장하고 관리하는 시스템입니다.
    /// [수정] 사용자별로 독립적인 세이브 파일을 생성하며, ID를 통한 UID 역추적을 지원합니다.
    /// </summary>
    public sealed class UserDataSystem : SingletonBase<UserDataSystem>
    {
        private string SaveDirectory => Path.Combine(Application.persistentDataPath, "SaveData");
        
        // 기본 경로는 유지하되, 데이터가 있을 때만 유효합니다.
        public string SavePath => Path.Combine(SaveDirectory, "UserData.json");

        protected override void OnInitialize()
        {
            EnsureDirectoryExists();
        }

        private string GetFilePath(string userName) => Path.Combine(SaveDirectory, $"UserData_{userName}.json");

        /// <summary>
        /// 전달받은 UserData 객체를 사용자별 파일로 저장합니다.
        /// </summary>
        public void SaveUserData(UserData data)
        {
            if (data == null || string.IsNullOrEmpty(data.userName)) return;

            try
            {
                EnsureDirectoryExists();

                string path = GetFilePath(data.userName);
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
                
                // 레거시 호환성을 위해 기본 파일에도 복사본을 남깁니다.
                File.WriteAllText(SavePath, json);
                
                Debug.Log($"[UserDataSystem] 데이터 저장 성공: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserDataSystem] 데이터 저장 오류: {e.Message}");
            }
        }

        /// <summary>
        /// 특정 사용자의 데이터를 로드합니다.
        /// </summary>
        public UserData LoadUserData(string userName)
        {
            string path = GetFilePath(userName);
            if (File.Exists(path) == false) path = SavePath; // Fallback to default

            if (File.Exists(path) == false) return null;

            try
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<UserData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserDataSystem] 데이터 로드 오류: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 레거시 로드 (호환성 유지)
        /// </summary>
        public UserData LoadUserData() => LoadUserData("Default");

        /// <summary>
        /// [핵심] 입력한 ID와 일치하는 로컬 세이브 파일에서 UID를 찾아 반환합니다.
        /// </summary>
        public string FindUIDById(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return string.Empty;

            UserData data = LoadUserData(userName);
            if (data != null && data.userName == userName)
            {
                Debug.Log($"[UserDataSystem] Local UID Recovery Success: {data.userUID}");
                return data.userUID;
            }

            Debug.LogWarning($"[UserDataSystem] No local data found for ID: {userName}");
            return string.Empty;
        }

        private void EnsureDirectoryExists()
        {
            if (Directory.Exists(SaveDirectory) == false)
            {
                Directory.CreateDirectory(SaveDirectory);
            }
        }
    }
}

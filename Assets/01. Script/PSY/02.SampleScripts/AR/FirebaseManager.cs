using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine.Serialization;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser currentUser;

    [SerializeField] private List<string> inventory = new List<string>();
    public List<string> MonsterInventory => inventory;
    public bool IsLoggedIn => currentUser != null;
    public string UserEmail => currentUser != null ? currentUser.Email : "Not Logged In";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(
            task => 
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;
                
                if (auth.CurrentUser != null)
                {
                    currentUser = auth.CurrentUser;
                    //LoadUserInventory();
                }
            }
            
            Debug.Log($"[Firebase] InitializeFirebase.Dependency Status: {dependencyStatus}");
        });
    }

    #region Auth Functions

    public async Task<bool> SignInAnonymously()
    {
        try
        {
            await auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"SignInAnonymously : {task.Exception}");
                    return;
                }

                var result = task.Result;
                FirebaseUser newUser = result.User; //Firebase의 유저 정보를 갖고 있는 유저 객체
            
                Debug.Log("<color=green> Firebase SignInAnonymously Success </color=green>");
                Debug.Log($"{newUser.UserId}, {newUser.DisplayName}, {newUser.Email}");
            });

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SignInAnonymously : {e.Message}");
            return false;
        }
        
        
        
    }
    
    public Task<bool> SignUp(string email, string password)
    {
        return auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled) return false;

            currentUser = task.Result.User;
            
            db.Collection("users").Document(currentUser.UserId).SetAsync(new Dictionary<string, object>
            {
                { "email", email },
                { "inventory", new List<string>() }
            });

            return true;
        });
    }

    public Task<bool> SignIn(string email, string password)
    {
        return auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled) return false;

            currentUser = task.Result.User;
            LoadUserInventory();

            return true;
        });
    }

    public void SignOut()
    {
        if (auth != null)
        {
            auth.SignOut();
            currentUser = null;
            inventory.Clear();
        }
    }

    #endregion

    #region Firestore Functions

    public Task LoadUserInventory()
    {
        if (currentUser == null) return Task.CompletedTask;

        return db.Collection("users").Document(currentUser.UserId).GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled) return;

            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists)
            {
                var data = snapshot.ToDictionary();
                if (data.ContainsKey("inventory") && data["inventory"] is List<object> list)
                {
                    inventory = list.Select(x => x.ToString()).ToList();
                }
            }
        });
    }

    public void AddMonsterToInventory(string monsterId)
    {
        if (currentUser == null) return;

        inventory.Add(monsterId);

        db.Collection("users").Document(currentUser.UserId)
            .UpdateAsync("inventory", inventory)
            .ContinueWithOnMainThread(task => {
                if (task.IsFaulted) Debug.LogError("Update Failed");
            });
    }

    #endregion
}

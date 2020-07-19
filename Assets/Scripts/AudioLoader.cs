using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
//using System.Windows.Forms;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
//using Application = UnityEngine.Application;

public class AudioLoader : MonoBehaviour
{
    public InputField songNameInput;
    public InputField songPathInput;

    public GameObject buttonsHolder;
    public GameObject coversHolder;
    public GameObject addButtonsHolder;
    public GameObject addPanel;
    public GameObject loadPanel;
    public GameObject savePanel;
    public GameObject browsePanel;

    public Button buttonPrefab;
    public Image coverPrefab;
    public Button addButton;

    //public Button editModeButton;
    //public Button playModeButton;

    public ScrollRect loadScrollView;
    public Button loadButtonPrefab;

    public InputField saveListNameInput;
    public ScrollRect saveScrollView;
    public Button saveButtonPrefab;

    private static readonly string filePrefix = "file://";
    //private static readonly string defaultSoundListFileName = "tests";
    private static readonly string defaultSoundListExtension = ".json";
    private SoundList soundList = new SoundList();

    private AudioSource audioSource;
    private List<AudioClip> audioClips = new List<AudioClip>();
    private int playIndex = 0;

    private int addIndex = -1;
    private GameObject addButtonClicked;
    private bool isEditMode = false;
    private string selectedLoadFileName = "";
    private string selectedSaveFileName = "";

    private static string STARRED_LIST_KEY = "Starred List";

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        HideAddPanel();
        HideBrowseMenu();
        HideLoadPanel();
        HideSavePanel();
        SetupEditMode();

        LoadStarredList();
    }

    public void HideSavePanel()
    {
        savePanel.SetActive(false);
    }

    public void ShowSavePanel()
    {
        RefreshSaveableFiles();
        savePanel.SetActive(true);
    }

    public void LoadSoundList()
    {
        string soundListPath = Path.Combine(Application.persistentDataPath, selectedLoadFileName + defaultSoundListExtension);
        if(File.Exists(soundListPath))
        {
            soundList = (SoundList)JsonSave<SoundList>.Load(soundListPath);

            if (soundList != null)
            {
                RefreshAddMode();
                HideEditMode();
                /*if (!isEditMode)
                {
                    SetupPlayMode();
                }
                audioClips = new List<AudioClip>();
                StartCoroutine(GetAudioClips());*/
                HideLoadPanel();
            }
        }
    }

    public void ShowLoadPanel()
    {
        RefreshLoadableFiles();
        loadPanel.SetActive(true);
    }

    public void HideLoadPanel()
    {
        loadPanel.SetActive(false);
    }

    private void RefreshLoadableFiles()
    {
        foreach (Transform button in loadScrollView.content)
        {
            Destroy(button.gameObject);
        }
        if (Directory.Exists(Application.persistentDataPath))
        {
            string[] soundFiles = Directory.GetFiles(Application.persistentDataPath, "*" + defaultSoundListExtension);
            for (int i = 0; i < soundFiles.Length; i++)
            {
                Button button = Instantiate(loadButtonPrefab);
                button.transform.SetParent(loadScrollView.content.transform, false);
                string fileName = Path.GetFileNameWithoutExtension(soundFiles[i]);
                button.GetComponent<Button>().onClick.AddListener(delegate { SetCurrentLoadName(fileName); });
                button.GetComponentInChildren<Text>().text = fileName;
            }
        }
    }

    private void RefreshSaveableFiles()
    {
        foreach (Transform button in saveScrollView.content)
        {
            Destroy(button.gameObject);
        }
        if (Directory.Exists(Application.persistentDataPath))
        {
            string[] soundFiles = Directory.GetFiles(Application.persistentDataPath, "*" + defaultSoundListExtension);
            for (int i = 0; i < soundFiles.Length; i++)
            {
                Button button = Instantiate(saveButtonPrefab);
                button.transform.SetParent(saveScrollView.content.transform, false);
                string fileName = Path.GetFileNameWithoutExtension(soundFiles[i]);
                button.GetComponent<Button>().onClick.AddListener(delegate { SetCurrentSaveName(fileName); });
                button.GetComponentInChildren<Text>().text = fileName;
            }
        }
    }

    private void SetCurrentLoadName(string name)
    {
        selectedLoadFileName = name;
    }

    private void SetCurrentSaveName(string name)
    {
        UpdateSelectedSaveName(name);
        saveListNameInput.text = selectedSaveFileName;
    }

    public void UpdateSelectedSaveName(string name)
    {
        selectedSaveFileName = name;
    }

    private void RefreshAddMode()
    {
        Button[] addButtons = addButtonsHolder.GetComponentsInChildren<Button>();
        for (int i = 0; i < 12; i++)
        {
            int tileIndex = soundList.Sounds.FindIndex(s => s.TilePosition == i + 1);
            if(tileIndex != -1)
            {
                addButtons[i].GetComponentInChildren<Text>().text = soundList.Sounds[tileIndex].Name;
            }
            else
            {
                addButtons[i].GetComponentInChildren<Text>().text = "";
            }
        }
    }

    public void SetupEditMode()
    {
        int maxRows = 4;
        int columns = 3;
        int tileSize = 170;
        int xOffset = 10;
        int yOffset = 50;
        //int left = addButtonsHolder.
        for (int i = 0; i < 12; i++)
        {
            int row = i / columns;
            int column = i % columns;
            int xPos = column * tileSize + xOffset;
            int yPos = (maxRows - row) * tileSize + yOffset;
            Vector2 position = new Vector2(xPos, yPos);

            Button button = Instantiate(addButton, addButtonsHolder.transform);
            //button.transform.SetParent(addButtonsHolder.transform, false);

            //Debug.Log("OLD (" + button.transform.position.x + ", " + button.transform.position.y + ")");
            button.transform.localPosition = position;
            //Debug.Log("NEW (" + button.transform.position.x + ", " + button.transform.position.y + ")");
            // tile index starts at 1
            var index = i + 1;
            button.onClick.AddListener(delegate { AddSoundAtTilePosition(index); });
        }
        ShowEditMode();
    }

    public void ShowEditMode()
    {
        // don't start edit mode if we are already in edit mode
        if(!isEditMode)
        {
            addButtonsHolder.SetActive(true);
            buttonsHolder.SetActive(false);
            coversHolder.SetActive(false);
            //editModeButton.gameObject.SetActive(false);
            //playModeButton.gameObject.SetActive(true);
            isEditMode = true;
        }
    }
    
    public void HideEditMode()
    {
        // don't start play mode if we are already in play mode
        if (isEditMode)
        {
            // loading all the sounds each time the mode changes is very inefficient
            audioClips = new List<AudioClip>();
            StartCoroutine(GetAudioClips());
            SetupPlayMode();
            addButtonsHolder.SetActive(false);
            buttonsHolder.SetActive(true);
            coversHolder.SetActive(true);
            //editModeButton.gameObject.SetActive(true);
            //playModeButton.gameObject.SetActive(false);
            isEditMode = false;
        }
    }

    private IEnumerator GetAudioClips(int startIndex = 0)
    {
        int index = startIndex;
        while(index < soundList.Sounds.Count)
        {
            string path = Path.Combine(filePrefix + soundList.Sounds[index].SoundPath);
            AudioType audioType = GetAudioType(path);
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, audioType))
            {
                yield return request.SendWebRequest();

                if (request.error != null)
                {
                    Debug.Log(request.error);
                }
                else
                {
                    audioClips.Add(DownloadHandlerAudioClip.GetContent(request));
                }

                index++;
            }
        }
    }

    private AudioType GetAudioType(string path)
    {
        string extension = Path.GetExtension(path).ToLower();
        extension = extension.Replace(".", "");
        //Debug.Log(extension);
        switch(extension)
        {
            case "ogg":
                return AudioType.OGGVORBIS;
            case "wav":
                return AudioType.WAV;
            case "aif":
            case "aiff":
                return AudioType.AIFF;
            case "mp3":
            case "m4a":
                return AudioType.MPEG;
            default:
                return AudioType.UNKNOWN;
        }
    }

    public void PlayAudioFile()
    {
        if(audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        audioSource.PlayOneShot(audioClips[playIndex]);
        playIndex++;
        if(playIndex == audioClips.Count)
        {
            playIndex = 0;
        }
    }

    public void SetupPlayMode()
    {
        foreach (Transform button in buttonsHolder.transform)
        {
            Destroy(button.gameObject);
        }

        foreach (Transform cover in coversHolder.transform)
        {
            Destroy(cover.gameObject);
        }

        addButtonsHolder.SetActive(false);
        int maxRows = 4;
        int columns = 3;
        //int[,] tilesMatrix = new int[3, 4];
        int tileSize = 170;
        int xOffset = 10;
        int yOffset = 50;
        for (int i = 0; i < 12; i++)
        {
            int row = i / columns;
            int column = i % columns;
            int xPos = column * tileSize + xOffset;
            int yPos = (maxRows - row) * tileSize + yOffset;
            Vector2 position = new Vector2(xPos, yPos);

            int tileIndex = soundList.Sounds.FindIndex(s => s.TilePosition == i + 1);
            if (tileIndex != -1)
            {
                Button button = Instantiate(buttonPrefab);
                button.transform.SetParent(buttonsHolder.transform, false);
                button.transform.localPosition = position;
                button.GetComponentInChildren<Text>().text = soundList.Sounds[tileIndex].Name;
                button.onClick.AddListener(delegate { PlayClipWithIndex(tileIndex); });
            }
            else
            {
                Image cover = Instantiate(coverPrefab);
                cover.transform.SetParent(coversHolder.transform, false);
                cover.transform.localPosition = position;
            }
        }
    }

    public void AddSoundAtTilePosition(int index)
    {
        addIndex = index;
        addButtonClicked = EventSystem.current.currentSelectedGameObject;
        int tileIndex = soundList.Sounds.FindIndex(s => s.TilePosition == index);
        //Debug.Log(addIndex);
        // Update text boxes to display the sound name and path
        if (tileIndex != -1)
        {
            songNameInput.text = soundList.Sounds[tileIndex].Name;
            songPathInput.text = soundList.Sounds[tileIndex].SoundPath;
        }
        else
        {
            songNameInput.text = "";
            songPathInput.text = "";
        }
        //songPathInput.caretPosition = songPathInput.text.Length;
        //songPathInput.ForceLabelUpdate();
        StartCoroutine(MoveInputTextToEnd());
        ShowAddMenu();
    }

    private IEnumerator MoveInputTextToEnd()
    {
        yield return new WaitForEndOfFrame();
        //Debug.Log(songPathInput.caretPosition);
        songPathInput.caretPosition = songPathInput.text.Length;
        
        songPathInput.MoveTextEnd(true);
        songPathInput.ForceLabelUpdate();
        //Debug.Log(songPathInput.caretPosition);
    }

    public void ShowAddMenu()
    {
        addPanel.SetActive(true);
    }

    public void HideAddPanel()
    {
        addPanel.SetActive(false);
    }

    public void ShowBrowseMenu()
    {
        browsePanel.SetActive(true);
    }

    public void HideBrowseMenu()
    {
        browsePanel.SetActive(false);
    }

    public void PlayClipWithIndex(int index)
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        audioSource.PlayOneShot(audioClips[index]);
    }

    public void SaveSoundList()
    {
        if(selectedSaveFileName.Length > 0)
        {
            string savePath = Path.Combine(Application.persistentDataPath, selectedSaveFileName + defaultSoundListExtension);
            if (File.Exists(savePath))
            {
                // ask user if he wants to overwrite using the confirmation panel
                Debug.Log("File exists");
            }
            else
            {
                // place save call here
            }
            // save here for now, or we won't be able to override lists
            JsonSave<SoundList>.Save(soundList, savePath);
            HideSavePanel();
        }
        else
        {
            // show error that a name has to be entered
        }
    }

    /*public void BrowseToSound()
    {
        string path = EditorUtility.OpenFilePanel("Select song", "C:/Users/jorda/Desktop/announcer", "ogg");
        if (path.Length != 0)
        {
            songNameInput.text = Path.GetFileNameWithoutExtension(path);
            songPathInput.text = path;
            //songPathInput.caretPosition = songPathInput.text.Length;
            //songPathInput.ForceLabelUpdate();
            StartCoroutine(MoveInputTextToEnd());
        }
    }*/

    public void AddSound()
    {
        string songName = songNameInput.text;
        string songPath = songPathInput.text;
        if(songName.Length != 0 && songPath.Length != 0)
        {
            // check if a sound already exists at the current tile position
            // if it does, replace it it the list instead of adding a new one
            int tileIndex = soundList.Sounds.FindIndex(s => s.TilePosition == addIndex);
            if(tileIndex != -1)
            {
                // the tile position remains the same since they were overlapping
                soundList.Sounds[tileIndex].Name = songName;
                soundList.Sounds[tileIndex].SoundPath = songPath;
            }
            else
            {
                soundList.Sounds.Add(new ExternalSound(songName, addIndex, songPath));
            }
            addButtonClicked.GetComponentInChildren<Text>().text = songName;
            HideAddPanel();
        }
    }

    public void RemoveSound()
    {
        songNameInput.text = "";
        songPathInput.text = "";

        int tileIndex = soundList.Sounds.FindIndex(s => s.TilePosition == addIndex);
        if (tileIndex != -1)
        {
            soundList.Sounds.RemoveAt(tileIndex);
            addButtonClicked.GetComponentInChildren<Text>().text = "";
        }
    }

    public void StarList()
    {
        //string soundListPath = Path.Combine(Application.persistentDataPath, selectedLoadFileName + defaultSoundListExtension);
        
        if (PlayerPrefs.HasKey(STARRED_LIST_KEY))
        {
            if(selectedLoadFileName == PlayerPrefs.GetString(STARRED_LIST_KEY))
            {
                PlayerPrefs.DeleteKey(STARRED_LIST_KEY);
                PlayerPrefs.Save();
                return;
            }
        }
        PlayerPrefs.SetString(STARRED_LIST_KEY, selectedLoadFileName);
        PlayerPrefs.Save();
    }

    private void LoadStarredList()
    {
        if (PlayerPrefs.HasKey(STARRED_LIST_KEY))
        {
            selectedLoadFileName = PlayerPrefs.GetString(STARRED_LIST_KEY);
            LoadSoundList();
        }
    }
}

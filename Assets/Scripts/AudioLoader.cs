using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

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
    public GameObject aboutPanel;

    public Button buttonPrefab;
    public Image coverPrefab;
    public Button addButton;

    public ScrollRect loadScrollView;
    public Button loadButtonPrefab;

    public InputField saveListNameInput;
    public ScrollRect saveScrollView;
    public Button saveButtonPrefab;

    private static readonly string filePrefix = "file://";
    private static readonly string defaultSoundListExtension = ".json";
    private SoundList soundList = new SoundList();

    private AudioSource audioSource;
    private AudioClip[] audioClips = new AudioClip[12];
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
        HideAboutPanel();
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

    public void HideAboutPanel()
    {
        aboutPanel.SetActive(false);
    }

    public void ShowAboutPanel()
    {
        aboutPanel.SetActive(true);
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
                if (!isEditMode)
                {
                    SetupPlayMode();
                }
                audioClips = new AudioClip[12];
                StartCoroutine(GetAudioClips());
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
        for (int i = 0; i < 12; i++)
        {
            int row = i / columns;
            int column = i % columns;
            int xPos = column * tileSize + xOffset;
            int yPos = (maxRows - row) * tileSize + yOffset;
            Vector2 position = new Vector2(xPos, yPos);

            Button button = Instantiate(addButton, addButtonsHolder.transform);

            button.transform.localPosition = position;
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
            isEditMode = true;
        }
    }
    
    public void HideEditMode()
    {
        // don't start play mode if we are already in play mode
        if (isEditMode)
        {
            SetupPlayMode();
            addButtonsHolder.SetActive(false);
            buttonsHolder.SetActive(true);
            coversHolder.SetActive(true);
            isEditMode = false;
        }
    }

    private IEnumerator GetAudioClips()
    {
        int index = 0;
        while(index < soundList.Sounds.Count)
        {
            string path = Path.Combine(filePrefix + soundList.Sounds[index].SoundPath);
            AudioType audioType = GetAudioType(path);
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, audioType))
            {
                yield return request.SendWebRequest();

                if (request.error != null)
                {
                    audioClips[index] = null;
                }
                else
                {
                    audioClips[index] = DownloadHandlerAudioClip.GetContent(request);
                }

                index++;
            }
        }
    }

    private IEnumerator GetAudioClip(int index)
    {
        string path = Path.Combine(filePrefix + soundList.Sounds[index].SoundPath);
        AudioType audioType = GetAudioType(path);
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, audioType))
        {
            yield return request.SendWebRequest();

            if (request.error != null)
            {
                audioClips[index] = null;
            }
            else
            {
                audioClips[index] = DownloadHandlerAudioClip.GetContent(request);
            }
        }
    }

    private AudioType GetAudioType(string path)
    {
        string extension = Path.GetExtension(path).ToLower();
        extension = extension.Replace(".", "");
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
        if(audioClips[playIndex] != null)
        {
            audioSource.PlayOneShot(audioClips[playIndex]);
        }
        playIndex++;
        if(playIndex == audioClips.Length)
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
        ShowAddMenu();
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
        if(audioClips[index] != null)
        {
            audioSource.PlayOneShot(audioClips[index]);
        }
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

                // if the sound path is the same, the sound doesn't need to be updated, just the song name
                // the song name gets updated every time
                soundList.Sounds[tileIndex].Name = songName;
                if (songPath != soundList.Sounds[tileIndex].SoundPath)
                {
                    soundList.Sounds[tileIndex].SoundPath = songPath;
                    StartCoroutine(GetAudioClip(tileIndex));
                }
            }
            else
            {
                soundList.Sounds.Add(new ExternalSound(songName, addIndex, songPath));
                StartCoroutine(GetAudioClip(soundList.Sounds.Count-1));
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
            audioClips[tileIndex] = null;
            addButtonClicked.GetComponentInChildren<Text>().text = "";
        }
    }

    public void StarList()
    {
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

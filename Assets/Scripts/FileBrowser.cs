using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FileBrowser : MonoBehaviour
{
    public string extensionFilter = "";
    public ScrollRect fileScroll;
    public InputField pathInput;
    public Button filePrefab;
    public Sprite folderIcon;
    public Sprite fileIcon;
    public GameObject fileBrowserHolder;

    public InputField songNameInput;
    public InputField songPathInput;

    public List<GameObject> scrollRedirectors = new List<GameObject>();

    private static string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    //private static string defaultPath = "C:/Users/jorda/Desktop/announcer";
    private string currentFileSelection = "";
    //private string startingFilePath = defaultPath;

    private string pickedFilePath = "";

    private static string START_PATH_KEY = "Start Path";
    //public UnityEvent<string> FileWasPicked;

    private void Start()
    {
        if (PlayerPrefs.HasKey(START_PATH_KEY))
        {
            defaultPath = PlayerPrefs.GetString(START_PATH_KEY);
        }

        foreach (var obj in scrollRedirectors)
        {
            RedirectScrollEvent(obj);
        }
    }

    private void RedirectScrollEvent(GameObject obj)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>();

        EventTrigger.Entry entryScroll = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Scroll
        };
        entryScroll.callback.AddListener((data) => { fileScroll.OnScroll((PointerEventData)data); });

        trigger.triggers.Add(entryScroll);
    }

    public void UpdateFileBrowser()
    {
        if(Directory.Exists(defaultPath))
        {
            currentFileSelection = "";
            UpdateCurrentFolderDisplay(defaultPath);
            fileScroll.verticalScrollbar.value = 1;
            ClearFilesDisplay();

            string[] subdirectories = Directory.GetDirectories(defaultPath);
            foreach (string subdirectory in subdirectories)
            {
                if(!FileHidden(subdirectory))
                {
                    InstantiateFilePrefab(subdirectory);
                }
            }

            //string searchPattern;
            var files = Directory.EnumerateFiles(defaultPath, "*.*");
            if (!String.IsNullOrEmpty(extensionFilter))
            {
                files = files.Where(f => extensionFilter.Contains("|" + Path.GetExtension(f).ToLower() + "|"));
            }
            
            //var files = Directory.EnumerateFiles(defaultPath, "*.*").Where(f => extensionFilter.Contains(Path.GetExtension(f).ToLower()));
            foreach (string file in files)
            {
                if (!FileHidden(file))
                {
                    InstantiateFilePrefab(file, true);
                }
            }
        }
    }

    private void UpdateCurrentFolderDisplay(string path)
    {
        pathInput.text = path;
        pathInput.caretPosition = pathInput.text.Length;
    }

    private bool FileHidden(string path)
    {
        FileInfo fi = new FileInfo(path);

        return (fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
    }

    public void NavigateToFile(string path)
    {
        if (File.Exists(path))
        {
            currentFileSelection = path;
            defaultPath = Path.GetDirectoryName(path);
            UpdateFileBrowser();
            DisplaySelectedFilePath(path);
        }
        else if (Directory.Exists(path))
        {
            currentFileSelection = "";
            defaultPath = path;
            UpdateFileBrowser();
            DisplaySelectedFilePath(path);
        }
        else
        {
            UpdateCurrentFolderDisplay(defaultPath);
        }
    }

    private void ClearFilesDisplay()
    {
        foreach (Transform button in fileScroll.content)
        {
            Destroy(button.gameObject);
        }
    }

    private void InstantiateFilePrefab(string path, bool isFile = false)
    {
        Button button = Instantiate(filePrefab);

        string fileName = Path.GetFileName(path);
        if(String.IsNullOrEmpty(fileName))
        {
            // if the last character is a backslash, it is probably a drive
            int index = path.LastIndexOf('\\');
            if(index == path.Length - 1)
            {
                fileName = path;
            }
            else
            {
                fileName = path.Substring(index + 1);
            }
        }

        EventTrigger trigger = button.GetComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        entry.callback.AddListener((data) => { OnPointerClickDelegate((PointerEventData)data, path); });
        trigger.triggers.Add(entry);

        EventTrigger.Entry entryScroll = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Scroll
        };
        entryScroll.callback.AddListener((data) => { fileScroll.OnScroll((PointerEventData)data); });
        trigger.triggers.Add(entryScroll);

        button.GetComponentInChildren<Text>().text = fileName;
        if (isFile)
        {
            button.GetComponentsInChildren<Image>()[1].sprite = fileIcon;
        }

        button.transform.SetParent(fileScroll.content.transform, false);
    }

    public void OnPointerClickDelegate(PointerEventData eventData, string fileName)
    {
        if (eventData.clickCount == 1)
        {
            currentFileSelection = fileName;
        }
        else if (eventData.clickCount == 2)
        {
            DisplaySelectedFilePath(fileName);
        }
    }

    public void DisplaySelectedFilePath(string filePath = "")
    {
        string path;
        if(String.IsNullOrEmpty(filePath))
        {
            path = currentFileSelection;
        }
        else
        {
            path = filePath;
        }
        if (File.Exists(path))
        {
            // This path is a file
            if (path.Length != 0)
            {
                songNameInput.text = Path.GetFileName(path);
                songPathInput.text = path;
            }
            pickedFilePath = path;
            //FileWasPicked.Invoke(pickedFilePath);
            Hide();
        }
        else if (Directory.Exists(path))
        {
            // This path is a directory
            defaultPath = path;
            UpdateFileBrowser();
        }
    }




    public void Show()
    {
        fileBrowserHolder.SetActive(true);
    }

    public void Hide()
    {
        fileBrowserHolder.SetActive(false);
    }

    public string GetPickedFilePath()
    {
        return pickedFilePath;
    }

    public void MoveUpFolder()
    {
        try
        {
            DirectoryInfo directoryInfo = Directory.GetParent(defaultPath);
            // directory info becomes null if we try to move up from C
            if(directoryInfo != null)
            {
                defaultPath = directoryInfo.FullName;
                UpdateFileBrowser();
            }
            else
            {
                ShowDrives();
            }
        }
        catch (ArgumentNullException)
        {
            Debug.Log("Path is a null reference.");
        }
        catch (ArgumentException)
        {
            Debug.Log("Path is an empty string, contains only white spaces, or contains invalid characters.");
        }
    }

    private void ShowDrives()
    {
        ClearFilesDisplay();
        UpdateCurrentFolderDisplay("");

        InstantiateFilePrefab(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), false);
        InstantiateFilePrefab(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), false);

        DriveInfo[] allDrives = DriveInfo.GetDrives();
        foreach (DriveInfo d in allDrives)
        {
            InstantiateFilePrefab(d.Name, false);
        }
    }

    public void SetRoot()
    {
        if(File.Exists(defaultPath))
        {
            PlayerPrefs.SetString(START_PATH_KEY, Path.GetDirectoryName(defaultPath));
            //startingFilePath = Path.GetDirectoryName(defaultPath);
        }
        else if(Directory.Exists(defaultPath))
        {
            PlayerPrefs.SetString(START_PATH_KEY, defaultPath);
            //startingFilePath = defaultPath;
        }
        PlayerPrefs.Save();
    }

    public void GoToRoot()
    {
        if(PlayerPrefs.HasKey(START_PATH_KEY))
        {
            DisplaySelectedFilePath(PlayerPrefs.GetString(START_PATH_KEY));
        }
    }
}

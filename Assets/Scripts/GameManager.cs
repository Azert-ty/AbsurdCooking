using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private bool objectiveCollected;
    void Awake()
    {
        Instance = this;
    }

    public void SetObjectiveCollected()
    {
        objectiveCollected = true;
    }

    public bool HasObjective()
    {
        return objectiveCollected;
    }
    

        public void Victory()
        {
            Debug.Log("VICTORY");
        }
    public void GameOver()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
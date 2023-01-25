using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

public class TournamentManager : UnitySingleton<TournamentManager>
{
    private enum StartOptions {
        PlayAI,
        Observation,
        TrainBasic,
        TrainScored,
        TrainOuts,
        TrainHuman,
        ImitationLearning
    }

    [SerializeField] private GameObject buildOptions;
    [SerializeField] private GameObject editorOptions;
    private bool humanPlayer;

    private void Awake() {
#if UNITY_EDITOR
        buildOptions.SetActive(false);
        editorOptions.SetActive(true);
#else
        buildOptions.SetActive(true);
        editorOptions.SetActive(false);
#endif
    }

    public void StartTournament(int choice) {
        SceneManager.LoadScene(choice);
    }

    public void Quit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit;
#endif
    }
}

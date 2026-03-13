using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var node = other.GetComponent<Node>();
        if (node == null) return;

        if (LevelManager.Instance != null &&
            LevelManager.Instance.CurrentState == LevelState.Run &&
            ConnectionManager.Instance != null &&
            ConnectionManager.Instance.AreAllNodesConnected())
        {
            LevelManager.Instance.EnterVictoryMode();
        }
    }
}

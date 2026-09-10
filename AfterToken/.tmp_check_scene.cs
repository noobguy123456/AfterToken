var playing = UnityEditor.EditorApplication.isPlaying;
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
var comp = UnityEngine.Object.FindObjectOfType<GameLogic.CompanionEntity>();
return "playing=" + playing + " scene=" + scene + " companion=" + (comp != null);

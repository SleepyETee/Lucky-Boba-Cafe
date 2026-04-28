using UnityEngine;

public enum StressTestMode
{
  None,
  InstantiateDestroy,
  CalculateDistance,
  TouchAllGameObjects
}
public class StressTest : MonoBehaviour
{
  [SerializeField]
  int _outerLoop = 10, _innerLoop = 10;
  [SerializeField]
  [Tooltip("What operation will be performed?")]
  StressTestMode _mode = StressTestMode.InstantiateDestroy;
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {
    for (int i = 0; i < _outerLoop; i++)
    {
      for (int j = 0; j < _innerLoop; j++)
      {
        switch (_mode)
        {
          case StressTestMode.InstantiateDestroy:
            var go = new GameObject("StressTest");
            Destroy(go);
            break;
          case StressTestMode.CalculateDistance:
            float f = Vector3.Distance(transform.position, Random.insideUnitSphere);
            break;
          case StressTestMode.TouchAllGameObjects:
            foreach (var g in FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID))
            {
              var n = g.name;
            }
            break;
          default:
            break;
        }
      }
    }
  }
}


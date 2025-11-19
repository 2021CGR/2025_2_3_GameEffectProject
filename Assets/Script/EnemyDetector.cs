using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 일정 반경 내의 적을 탐지하고, 가장 가까운 적을 찾아주는 클래스입니다.
public class EnemyDetector : MonoBehaviour
{
    // [설정] 적을 감지할 반경 (기본값 10)
    [SerializeField] private float detectionRadius = 10.0f;

    // [설정] 적으로 인식할 레이어 (이 레이어가 설정된 오브젝트만 감지함)
    [SerializeField] private LayerMask enemyLayer;


    // 범위 내에서 가장 가까운 적 하나를 반환하는 함수입니다.
    public GameObject GetClosestEnemy()
    {
        // 현재 위치(transform.position)를 중심으로 반지름(detectionRadius)만큼의 구체(Sphere)를 그려,
        // 그 안에 들어온 enemyLayer를 가진 모든 콜라이더를 배열로 가져옵니다.
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);

        // 감지된 적이 하나라도 있다면
        if (enemiesInRange.Length > 0)
        {
            GameObject bestTarget = null; // 가장 가까운 적을 저장할 변수
            float closestDistanceSqr = Mathf.Infinity; // 가장 가까운 거리값 (초기값은 무한대)
            Vector3 currentPosition = transform.position; // 탐지하는 주체의 현재 위치

            // 감지된 모든 적들을 하나씩 검사합니다.
            foreach (Collider enemyCollider in enemiesInRange)
            {
                // 자기 자신은 적으로 간주하지 않고 건너뜁니다.
                if (enemyCollider.gameObject == this.gameObject)
                    continue;

                // 타겟까지의 방향 벡터를 구합니다.
                Vector3 directionToTarget = enemyCollider.transform.position - currentPosition;

                // 거리의 제곱(sqrMagnitude)을 구합니다. 
                // (Vector3.Distance보다 연산 속도가 빨라서 거리 비교용으로 주로 사용합니다)
                float dSqrToTarget = directionToTarget.sqrMagnitude;

                // 현재까지 찾은 가장 가까운 거리보다 더 가깝다면 정보를 갱신합니다.
                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = enemyCollider.gameObject;
                }
            }
            // 최종적으로 가장 가까운 적(GameObject)을 반환합니다.
            return bestTarget;
        }
        else
        {
            // 범위 내에 적이 없으면 null 반환
            return null;
        }
    }

    // 범위 내에 있는 모든 적들의 리스트를 반환하는 함수입니다.
    public List<GameObject> GetEnemiesInRange()
    {
        List<GameObject> enemiesList = new List<GameObject>();

        // 위와 마찬가지로 주변의 적 콜라이더들을 가져옵니다.
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);

        foreach (Collider enemyCollider in enemiesInRange)
        {
            // 자기 자신이 아닌 경우에만 리스트에 추가합니다.
            if (enemyCollider.gameObject != this.gameObject)
            {
                enemiesList.Add(enemyCollider.gameObject);
            }
        }

        // 적들의 리스트를 반환합니다.
        return enemiesList;
    }
}
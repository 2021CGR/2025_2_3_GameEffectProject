using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ChainShoot : MonoBehaviour
{
    // [설정 변수들]
    [Header("Settings")]
    [SerializeField, Tooltip("라인 렌더러가 갱신되는 주기 (초 단위)")]
    float refreshRate = 0.01f;

    [SerializeField, Range(1, 10), Tooltip("최대 연결 가능한 적의 수")]
    int maximunEnemiesInChain = 3;

    [SerializeField, Tooltip("다음 적으로 연결되기 전 대기 시간")]
    float delayBetweenEachChain = 0.3f;

    // [참조 변수들]
    [Header("References")]
    [SerializeField, Tooltip("플레이어의 발사 위치")]
    Transform playerFirePoint;

    [SerializeField, Tooltip("플레이어의 적 감지 스크립트")]
    EnemyDetector playerEnemyDetector;

    [SerializeField, Tooltip("생성할 라인 렌더러 프리팹")]
    GameObject lineRendererPrefab;

    // [내부 상태 변수들]
    bool shooting;      // 현재 사격 중인지 여부
    bool shot;          // 발사가 시작되었는지 여부
    float counter = 1;  // 현재 연결된 체인 카운트
    GameObject currentClosestEnemy; // 현재 가장 가까운 적

    // 생성된 객체 관리 리스트
    List<GameObject> spawnedLineRenderers = new List<GameObject>();
    List<GameObject> enemiesInChain = new List<GameObject>();
    List<GameObject> activeEffect = new List<GameObject>();

    // ---------------------------------------------------------
    // [추가됨] 사격을 시작하는 함수
    // 원래 코드에 없어서 Update에서 오류가 나던 부분을 구현했습니다.
    // ---------------------------------------------------------
    void StartShooting()
    {
        shooting = true;
        shot = true;

        // 현재 플레이어 기준 가장 가까운 적을 찾음
        currentClosestEnemy = playerEnemyDetector.GetClosestEnemy();

        if (currentClosestEnemy != null)
        {
            // 체인 리스트에 첫 번째 적 추가
            enemiesInChain.Add(currentClosestEnemy);

            // 플레이어 -> 첫 번째 적을 잇는 라인 생성 (true는 플레이어 기준 추적을 의미)
            NewLineRenderer(playerFirePoint, currentClosestEnemy.transform, true);

            // 연쇄 반응 코루틴 시작
            StartCoroutine(ChainReaction(currentClosestEnemy));
        }
        else
        {
            // 적이 없으면 사격 중지
            StopShooting();
        }
    }

    // 사격을 중지하고 모든 효과를 정리하는 함수 (중복된 것 중 하나를 제거하고 정리함)
    void StopShooting()
    {
        shooting = false;
        shot = false;
        counter = 1; // 카운터 초기화

        // 생성된 라인 렌더러들 모두 제거
        for (int i = 0; i < spawnedLineRenderers.Count; i++)
        {
            if (spawnedLineRenderers[i] != null)
                Destroy(spawnedLineRenderers[i]);
        }

        spawnedLineRenderers.Clear();
        enemiesInChain.Clear();

        // 활성화된 이펙트들 제거 (현재 코드상 추가 로직은 없으나 구조 유지)
        for (int i = 0; i < activeEffect.Count; i++)
        {
            if (activeEffect[i] != null)
                Destroy(activeEffect[i]);
        }

        activeEffect.Clear();
    }

    // 라인 렌더러의 위치를 계속 업데이트하는 코루틴 (재귀 호출 방식 유지)
    IEnumerator UpdateLineRenderer(GameObject lineR, Transform startPos, Transform endPos, bool getClosestEnemyToPlayer = false)
    {
        // 사격 중이고, 라인 렌더러가 존재할 때만 실행
        if (shooting && shot && lineR != null)
        {
            // 라인 렌더러 컨트롤러를 통해 시작점과 끝점 설정
            // 주의: LineRendererController라는 별도 스크립트가 lineRendererPrefab에 있어야 함
            if (lineR.GetComponent<LineRendererController>() != null)
            {
                lineR.GetComponent<LineRendererController>().SetPosition(startPos, endPos);
            }

            yield return new WaitForSeconds(refreshRate);

            // 플레이어와 가장 가까운 적을 잇는 라인인 경우 (첫 번째 라인)
            if (getClosestEnemyToPlayer)
            {
                // 적이 사라지거나 변경되었는지 체크
                if (playerEnemyDetector.GetClosestEnemy() != null)
                {
                    StartCoroutine(UpdateLineRenderer(lineR, startPos, playerEnemyDetector.GetClosestEnemy().transform, true));

                    // 타겟이 변경되었다면 사격 재설정 (끊고 다시 쏘기 등)
                    if (currentClosestEnemy != playerEnemyDetector.GetClosestEnemy())
                    {
                        StopShooting();
                        // StartShooting(); // 필요 시 주석 해제하여 자동 재발사 가능
                    }
                }
                else
                {
                    StopShooting(); // 적이 없어지면 중지
                }
            }
            else
            {
                // 적과 적 사이를 잇는 라인인 경우 (타겟이 존재할 때만)
                if (endPos != null)
                {
                    StartCoroutine(UpdateLineRenderer(lineR, startPos, endPos));
                }
                else
                {
                    // 연결된 적이 사라지면 해당 라인 파괴 (선택 사항)
                    Destroy(lineR);
                }
            }
        }
    }

    // 새로운 라인 렌더러를 생성하고 업데이트를 시작하는 함수
    void NewLineRenderer(Transform startPos, Transform endPos, bool getClosestEnemyToPlayer = false)
    {
        GameObject lineR = Instantiate(lineRendererPrefab);
        spawnedLineRenderers.Add(lineR);
        StartCoroutine(UpdateLineRenderer(lineR, startPos, endPos, getClosestEnemyToPlayer));
    }

    // 적들끼리 연결되는 연쇄 반응 코루틴
    IEnumerator ChainReaction(GameObject closesEnemey)
    {
        yield return new WaitForSeconds(delayBetweenEachChain);

        // 최대 체인 수에 도달했으면 종료
        if (counter >= maximunEnemiesInChain) // == 대신 >= 가 더 안전함
        {
            yield return null;
        }
        else
        {
            if (shooting && closesEnemey != null)
            {
                // 현재 적(closesEnemey)이 감지한 '그 다음으로 가장 가까운 적'을 가져옴
                EnemyDetector enemyDetector = closesEnemey.GetComponent<EnemyDetector>();

                if (enemyDetector != null)
                {
                    GameObject nextTarget = enemyDetector.GetClosestEnemy();

                    // 다음 타겟이 있고, 이미 체인에 포함된 적이 아니라면 연결
                    if (nextTarget != null && !enemiesInChain.Contains(nextTarget))
                    {
                        counter++;
                        enemiesInChain.Add(nextTarget); // 리스트에 추가

                        // 현재 적 -> 다음 적 라인 생성
                        NewLineRenderer(closesEnemey.transform, nextTarget.transform);

                        // 다음 적 기준으로 다시 연쇄 반응 시작 (재귀)
                        StartCoroutine(ChainReaction(nextTarget));
                    }
                }
            }
        }
    }

    void Update()
    {
        // 발사 버튼(좌클릭 등)을 누르고 있을 때
        if (Input.GetButton("Fire1"))
        {
            // 감지된 적이 있을 때만
            if (playerEnemyDetector.GetEnemiesInRange().Count > 0)
            {
                if (!shooting)
                {
                    StartShooting(); // 이제 이 함수가 존재하므로 오류가 나지 않음
                }
            }
            else
            {
                // 범위 내 적이 없으면 멈춤
                StopShooting();
            }
        }

        // 버튼을 뗐을 때 사격 중지
        if (Input.GetButtonUp("Fire1"))
        {
            StopShooting();
        }
    }
}
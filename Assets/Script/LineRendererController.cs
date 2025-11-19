using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 라인 렌더러(레이저 효과)의 시작점과 끝점을 제어하는 클래스입니다.
public class LineRendererController : MonoBehaviour
{
    // [설정] 제어할 라인 렌더러 컴포넌트들의 리스트입니다.
    // 여러 개의 라인 렌더러를 겹쳐서 더 화려한 효과를 낼 때 유용합니다.
    [SerializeField] List<LineRenderer> lineRenderers = new List<LineRenderer>();

    // 시작점(startPos)과 목표점(endPos)을 받아 라인을 그리는 함수입니다.
    public void SetPosition(Transform startPos, Transform endPos)
    {
        // 리스트에 라인 렌더러가 하나라도 있다면 실행
        if (lineRenderers.Count > 0)
        {
            // 등록된 모든 라인 렌더러를 순회하며 위치를 업데이트합니다.
            for (int i = 0; i < lineRenderers.Count; i++)
            {
                // 라인 렌더러의 점 개수가 2개 이상인지 확인합니다. (선은 최소 2개의 점이 필요하므로)
                if (lineRenderers[i].positionCount >= 2)
                {
                    // 0번 인덱스: 선의 시작 위치
                    lineRenderers[i].SetPosition(0, startPos.position);
                    // 1번 인덱스: 선의 끝 위치
                    lineRenderers[i].SetPosition(1, endPos.position);
                }
            }
        }
    }
}
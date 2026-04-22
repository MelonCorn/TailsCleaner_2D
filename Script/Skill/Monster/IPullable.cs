using UnityEngine;

public interface IPullable
{
    /// <summary>
    /// 목표 지점으로 끌어당기기
    /// </summary>
    void Pull(Vector2 targetPosition);
}
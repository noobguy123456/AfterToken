using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 队友倒地状态：停止一切行为，视觉变灰。MVP 不做救援，撤离/重开场景后重新生成。
    /// </summary>
    public class CompanionDeadState : CompanionStateBase
    {
        public override string StateName => "Dead";

        protected override void OnEnterState(IFsm<CompanionEntity> fsm)
        {
            StopMoving();

            // 倒地视觉：胶囊放平 + 变灰
            var visual = Owner.transform.Find("Visual");
            if (visual != null)
            {
                visual.localRotation = Quaternion.Euler(80f, 0f, 0f);
                var renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.35f, 0.35f, 0.35f);
                }
            }
        }
    }
}

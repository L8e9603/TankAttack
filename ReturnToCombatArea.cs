using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReturnToCombatArea : MonoBehaviour
{
    private float countdown = 11f;
    private TankMove tankMove;
    private TurretControl turretControl;
    private CannonControl cannonControl;
    private FireCannon fireCannon;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Tank"))
        {
            countdown = 11f;
            UIManager.instance.SetActiveReturnToCombatAreaUI(true);
            UIManager.instance.returnToCombatAreaUI.GetComponentInChildren<Text>().text = "RETURN TO COMBAT AREA\n" + Mathf.Floor(countdown);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Tank"))
        {
            tankMove = other.GetComponent<TankMove>();
            turretControl = other.GetComponentInChildren<TurretControl>();
            cannonControl = other.GetComponentInChildren<CannonControl>();
            fireCannon = other.GetComponent<FireCannon>();

            ReturnToCombatAreaCountdown(); // 콜라이더에 접촉중인 상태라면 카운트다운 실행
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Tank"))
        {
            UIManager.instance.SetActiveReturnToCombatAreaUI(false);
        }
    }

    public void ReturnToCombatAreaCountdown()
    {
        if (countdown <= 0)
        {
            UIManager.instance.returnToCombatAreaUI.GetComponentInChildren<Text>().text = "DESERTED";

            // 탱크 조작 비활성화시키고, 3초 후 리스폰
            tankMove.h = Mathf.Lerp(tankMove.h, 0f, Time.deltaTime * 0.5f);
            tankMove.v = Mathf.Lerp(tankMove.v, 0f, Time.deltaTime * 0.5f);
            turretControl.enabled = false;
            cannonControl.enabled = false;
            fireCannon.enabled = false;
        }

        countdown -= Time.deltaTime;
        UIManager.instance.returnToCombatAreaUI.GetComponentInChildren<Text>().text = "RETURN TO COMBAT AREA\n" + Mathf.Floor(countdown);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Google.Protobuf.Protocol;

public class PlayerController : CreatureController
{
    protected Coroutine _coSkill;
    protected bool _rangeSkill = false;

    protected override void Init()
    {
        base.Init();
    }

    protected override void UpdateController()
    {
        base.UpdateController();
    }

    protected override void UpdateAnimation()
    {
        if (State == CreatureState.Idle)
        {
            switch (_lastDir)
            {
                case MoveDir.Up:
                    _animator.Play("Player_IdleBack");
                    _spriteRenderer.flipX = false;
                    break;

                case MoveDir.Down:
                    _animator.Play("Player_IdleFront");
                    _spriteRenderer.flipX = false;
                    break;
                case MoveDir.Left:
                    _animator.Play("Player_IdleSide");
                    _spriteRenderer.flipX = true;
                    break;
                case MoveDir.Right:
                    _animator.Play("Player_IdleSide");
                    _spriteRenderer.flipX = false;
                    break;
            }
        }
        else if (State == CreatureState.Moving)
        {
            switch (_lastDir)
            {
                case MoveDir.Up:
                    _animator.Play("Player_WalkBack");
                    _spriteRenderer.flipX = false;
                    break;
                case MoveDir.Down:
                    _animator.Play("Player_WalkFront");
                    _spriteRenderer.flipX = false;
                    break;
                case MoveDir.Left:
                    _animator.Play("Player_WalkSide");
                    _spriteRenderer.flipX = true;
                    break;
                case MoveDir.Right:
                    _animator.Play("Player_WalkSide");
                    _spriteRenderer.flipX = false;
                    break;
            }
        }
        else if (State == CreatureState.Skill)
        {
            switch (_lastDir)
            {
                case MoveDir.Up:
                    _animator.Play(_rangeSkill ? "Player_AttackWeaponBack" : "Player_AttackBack");
                    _spriteRenderer.flipX = false;
                    break;
                case MoveDir.Down:
                    _animator.Play(_rangeSkill ? "Player_AttackWeaponFront" : "Player_AttackFront");
                    _spriteRenderer.flipX = false;
                    break;
                case MoveDir.Left:
                    _animator.Play(_rangeSkill ? "Player_AttackWeaponSide" : "Player_AttackSide");
                    _spriteRenderer.flipX = true;
                    break;
                case MoveDir.Right:
                    _animator.Play(_rangeSkill ? "Player_AttackWeaponSide" : "Player_AttackSide");
                    _spriteRenderer.flipX = false;
                    break;
            }
        }
        else
        {

        }
    }

    protected override void UpdateIdle()
    {
        if (Dir != MoveDir.None)
        {
            State = CreatureState.Moving;
            return;
        }
    }

    public void UseSkill(int skillId)
    {
        if (skillId == 1)
        {
            _coSkill = StartCoroutine("CoStartPunch");
        }
    }

    IEnumerator CoStartPunch()
    {
        _rangeSkill = false;
        State = CreatureState.Skill;
        yield return new WaitForSeconds(0.5f);
        State = CreatureState.Idle;
        _coSkill = null;
        CheckUpdatedFlag();
    }

    protected virtual void CheckUpdatedFlag()
    { 
    
    }

    IEnumerator CoStartShootArrow()
    {
        GameObject go = Managers.Resource.Instantiate("Creature/Arrow");
        ArrowController ac = go.GetComponent<ArrowController>();
        ac.Dir = _lastDir;
        ac.CellPos = CellPos;

        _rangeSkill = true;
        yield return new WaitForSeconds(0.5f);
        State = CreatureState.Idle;
        _coSkill = null;
    }
}

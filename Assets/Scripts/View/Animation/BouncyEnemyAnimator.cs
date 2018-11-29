using UnityEngine;

public class BouncyEnemyAnimator : SpriteAnimator {
    public float fallSpeedThreshold;
    public float jumpMidPointSpeedThreshold;

    private State state;
    private JumpPhase jumpPhase;

    private JumpyEnemy enemy;

    public void Start() {
        this.enemy = this.gameObject.GetComponentInParent<JumpyEnemy>();
    }

    public void OnEnable() {
        this.state = State.Ground;
    }

    public void Refresh(Vector2 velocity, bool grounded, bool jumped, bool prepareForJump, bool landed, bool dead) {
        // change state:

        switch(this.state) {
            case State.Ground:
                {
                    if(jumped || (!grounded && Mathf.Abs(velocity.y) > this.fallSpeedThreshold)) {
                        this.state = State.Jump;
                    } else if(dead) {
                        this.state = State.Dead;
                    } else if(prepareForJump) {
                        this.state = State.JumpPrepare;
                    }
                }
                break;

            case State.JumpPrepare:
                {
                    if(jumped || (!grounded && Mathf.Abs(velocity.y) > this.fallSpeedThreshold)) {
                        this.state = State.Jump;
                    } else if(dead) {
                        this.state = State.Dead;
                    }
                }
                break;

            case State.Jump:
                {
                    if(landed || grounded) {
                        this.state = State.JumpLand;
                    }
                }
                break;

            case State.JumpLand:
                {
                    if(jumped || (!grounded && Mathf.Abs(velocity.y) > this.fallSpeedThreshold)) {
                        this.state = State.Jump;
                    } else if(!landed) {
                        this.state = State.Ground;
                    }
                }
                break;
        }

        // state logic:

        if(this.state == State.Ground || this.state == State.JumpLand) {
            this.transform.localPosition = new Vector3(0, 0.25f, 0);
        } else {
            this.transform.localPosition = new Vector3(0, 1.25f, 0);
        }

        switch(this.state) {
            case State.Jump:
                {
                    if(Mathf.Abs(velocity.y) < this.jumpMidPointSpeedThreshold) {
                        this.jumpPhase = JumpPhase.MidPoint;
                    } else if(velocity.y > 0) {
                        this.jumpPhase = JumpPhase.Up;
                    } else {
                        this.jumpPhase = JumpPhase.Down;
                    }
                }
                break;

            case State.Dead:
                {
                    if(this.finishedSingleShot) {
                        this.enemy.FullDeath();
                    }
                }
                break;
        }

        // set animation according to state:
        switch(this.state) {
            case State.Ground:
                {
                    SetActive("Ground");
                }
                break;

            case State.JumpPrepare:
                {
                    SetActive("JumpKick");
                }
                break;

            case State.Jump:
                {
                    switch(this.jumpPhase) {
                        case JumpPhase.Up:
                            SetActive("JumpUp");
                            break;
                        case JumpPhase.MidPoint:
                            SetActive("JumpMid");
                            break;
                        case JumpPhase.Down:
                            SetActive("JumpDown");
                            break;
                    }
                }
                break;

            case State.JumpLand:
                {
                    SetActive("JumpLand");
                }
                break;

            case State.Dead:
                {
                    SetActive("Death");
                }
                break;
        }
    }

    private enum State {
        Ground,

        JumpPrepare,
        Jump,
        JumpLand,

        Dead,
    }

    private enum JumpPhase {
        Up,
        MidPoint,
        Down,
    }
}
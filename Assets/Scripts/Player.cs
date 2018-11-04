﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour {
	// settings:
	[Header("Jump")]
	public float jumpSpeed;
	public int coyoteFrames, jumpAheadFrames;

	[Header("Movement")]
	public float moveSpeed;
	public float groundedAccelerationTime, airAccelerationTime;
	[Range(0, 1f)]
	public float wallSlideDamping = 0.9f;
	public float wallJumpAngle;

	// constants:
	private const float LOOK_AHEAD_DIST = 0.01f;
	private const float MIN_GROUND_NORMAL_Y = 0.65f;
	private const float MIN_WALL_NORMAL_X = 0.65f;

	// state:
	private Vector2 velocity;
	private float velocityXRef;

	private bool grounded;
	private bool wallSliding;
	private bool lastWasWall;
	private float wallNormalX;
	private int groundFrames = 100;
	private int jumpFrames = 100;

	// inputs:
	private Vector2 input;
	private bool jump;

	// misc:
	private Rigidbody2D rigid;

	private ContactFilter2D contactFilter;
	private RaycastHit2D[] raycastResults;

	public void Awake() {
		this.rigid = this.gameObject.GetComponent<Rigidbody2D>();

		this.contactFilter = new ContactFilter2D();
		this.contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(this.gameObject.layer));
		this.contactFilter.useLayerMask = true;
		this.contactFilter.useTriggers = false;

		this.raycastResults = new RaycastHit2D[32];
	}

	public void Update() {
		HandleInput();
	}

	public void FixedUpdate() {
		Vector2 deltaMovement = Vector2.zero;

		// handle vertical movement:

		this.grounded = false;

		this.velocity += Physics2D.gravity * Time.deltaTime;

		if(this.wallSliding && this.velocity.y < 0 && Mathf.Sign(input.x) == -Mathf.Sign(this.wallNormalX) && Mathf.Abs(input.x) > 0.1f) {
			this.velocity.y *= this.wallSlideDamping;
		}

		Vector2 deltaY = Vector2.up * this.velocity.y * Time.deltaTime;

		float dist = Mathf.Abs(deltaY.y);

		int c = this.rigid.Cast(deltaY, this.contactFilter, this.raycastResults, dist + LOOK_AHEAD_DIST);

		for(int i = 0; i < c; i++) {
			RaycastHit2D hit = this.raycastResults[i];
			Vector2 normal = hit.normal;

			if(normal.y > MIN_GROUND_NORMAL_Y) {
				this.grounded = true;
				this.lastWasWall = false;
				this.groundFrames = 0;
			}

			float p = Vector2.Dot(velocity, normal);
			if(p < 0) {
				this.velocity -= p * normal;
			}

			if(hit.distance - LOOK_AHEAD_DIST < dist) {
				dist = hit.distance - LOOK_AHEAD_DIST;
			}
		}

		if(dist > 0) {
			deltaMovement += deltaY.normalized * dist;
		}

		// handle horizontal movement:

		this.wallSliding = false;
		
		this.velocity.x = Mathf.SmoothDamp(this.velocity.x, this.input.x * this.moveSpeed, ref this.velocityXRef, this.grounded ? this.groundedAccelerationTime : this.airAccelerationTime);		
		
		Vector2 deltaX = Vector2.right * this.velocity.x * Time.deltaTime;

		dist = Mathf.Abs(deltaX.x);

		c = this.rigid.Cast(deltaX, this.contactFilter, this.raycastResults, dist + LOOK_AHEAD_DIST);

		for(int i = 0; i < c; i++) {
			RaycastHit2D hit = this.raycastResults[i];

			if(Mathf.Abs(hit.normal.x) > MIN_WALL_NORMAL_X && !this.grounded) {
				this.wallSliding = true;
				this.lastWasWall = true;
				this.groundFrames = 0;
				this.wallNormalX = hit.normal.x;
			}

			float p = Vector2.Dot(velocity, hit.normal);
			if(p < 0) {
				this.velocity -= p * hit.normal;
			}

			if(hit.distance - LOOK_AHEAD_DIST < dist) {
				dist = hit.distance - LOOK_AHEAD_DIST;
			}
		}

		if(dist > 0) {
			deltaMovement += deltaX.normalized * dist;
		}

		// actually move:
		dist = deltaMovement.magnitude;
		c = this.rigid.Cast(deltaMovement, this.contactFilter, this.raycastResults, dist + LOOK_AHEAD_DIST);

		for(int i = 0; i < c; i++) {
			RaycastHit2D hit = this.raycastResults[i];
			
			if(hit.distance <= dist) {
				float p = Vector2.Dot(deltaMovement, hit.normal);
				if(p < 0) {
					Debug.Log(p * hit.normal);
					deltaMovement -= p * hit.normal;
				}
			}
		}

		this.rigid.position += deltaMovement;

		// jump:

		if(this.grounded || this.wallSliding) {
			if(this.jump || this.jumpFrames < this.jumpAheadFrames) {
				Jump();
			}
		} else {
			if(this.jump && this.groundFrames < this.coyoteFrames) {
				Jump();
			}
		}

		// misc:

		ResetInput();
		
		this.jumpFrames++;
		this.groundFrames++;
	}

	private void Jump() {
		if(this.lastWasWall) { // wall jump
			this.velocity.y = 0;

			this.velocity.x += Mathf.Sign(this.wallNormalX) * Mathf.Cos(this.wallJumpAngle * Mathf.Deg2Rad) * this.jumpSpeed;
			this.velocity.y += Mathf.Sin(this.wallJumpAngle * Mathf.Deg2Rad) * this.jumpSpeed;
		} else {
			this.velocity.y += this.jumpSpeed;
		}

		this.jumpFrames += 100;
		this.groundFrames += 100;
	}

	private void HandleInput() {
		if(Input.GetKeyDown(KeyCode.Space)) {
			this.jump = true;
			
			this.jumpFrames = 0;
		} else {
			this.jump = false;
		}

		this.input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
	}

	private void ResetInput() {
		this.jump = false;
	}
}

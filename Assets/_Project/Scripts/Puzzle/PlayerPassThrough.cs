using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lets the players walk through this object's colliders while everything else (crates, the Heart Cart) still collides
/// with them. Used in the Heart Engine hall (finale, part 2) on the striker (the cart still slams into it, players just
/// walk past it) and on the cart stop beside the lift (it keeps the cart from being pushed off the lift to the east).
/// How: Physics2D.IgnoreCollision between these colliders and each player's colliders. Players are spawned at runtime
/// (LocalCoopSpawner), so it looks for them now and then. No layer or project setting is changed.
/// Setup: on an object with solid Collider2Ds.
/// </summary>
public class PlayerPassThrough : MonoBehaviour
{
    private Collider2D[] own = new Collider2D[0];
    private readonly HashSet<Collider2D> done = new HashSet<Collider2D>();
    private float timer;

    private void Awake()
    {
        own = GetComponents<Collider2D>();
    }

    private void Update()
    {
        if ((timer -= Time.deltaTime) > 0f) return;
        timer = 1f; // cheap: players rarely change
        foreach (PlayerMovement player in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
            foreach (Collider2D theirs in player.GetComponentsInChildren<Collider2D>())
            {
                if (theirs == null || done.Contains(theirs)) continue;
                foreach (Collider2D mine in own) if (mine != null) Physics2D.IgnoreCollision(mine, theirs, true);
                done.Add(theirs);
            }
    }
}

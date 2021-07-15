using FirstGearGames.Mirror.ColliderRollbacks;
using Mirror;
using UnityEngine;

public class RollbackTestPlayer : NetworkBehaviour
{
    #region Test code.
    public bool Run = true;
    [Range(0.001f, 0.55f)]
    public float RollbackTime = 0.15f;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            //Debug.Log("Firing Lazer.");

            RollbackManager.Rollback(RollbackTime, RollbackManager.PhysicsTypes.ThreeDimensional);

            Debug.DrawLine(transform.position, transform.position + (transform.forward * 10f), Color.red, 2f);
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, 10f))
                Debug.Log("Laser hit.");

            RollbackManager.ReturnForward();
        }

        //if (!Run)
        //    return;
        //RollbackManager.Rollback(new RollbackData(RollbackTime, HitLayer), RollbackManager.PhysicsTypes.ThreeDimensional);
        //RollbackManager.ReturnForward();

    }
    #endregion


    //#region Sample Code.
    //private NetworkTransform _netTransform;

    //public override void OnStartServer()
    //{
    //    base.OnStartServer();
    //    _netTransform = GetComponent<NetworkTransform>();
    //}

    //public void Update()
    //{
    //    if (base.isClient && base.hasAuthority)
    //    {
    //        /* Send fire with the current network time so the server
    //         * can calculate a rough packet delay time. */
    //        if (Input.GetKeyDown(KeyCode.Space))
    //            CmdFire(NetworkTime.time);
    //    }
    //}

    //[Command]
    //private void CmdFire(double networkTime)
    //{
    //    /* Get a rough amount of time to roll back.
    //     * This could be improved upon. */
    //    double time = _netTransform.syncInterval + (NetworkTime.time - networkTime);
    //    /* Only colliders setup using this layer will be rolled back.
    //     * You can also implement your own data to check against, such as team index, in
    //     * RollbackData. */
    //    LayerMask rayLayer = -1;

    //    //Perform a rollback.
    //    RollbackManager.Rollback(new RollbackData((float)time, rayLayer));

    //    /* #################################
    //     * PERFORM CAST HERE.
    //     * ################################# */

    //    //Return colliders forward.
    //    RollbackManager.ReturnForward();
    //}
    //#endregion

}

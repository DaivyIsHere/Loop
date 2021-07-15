using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FirstGearGames.Mirror.ColliderRollbacks
{


    public class Strafe : NetworkBehaviour
    {
        public bool FancyStrafe = true;

        public bool PerformRollbacks = true;
        public float MoveRate = 3f;
        public float RotateSpeed = 15f;

        private Vector3[] _goals;
        private bool _moveRight = false;

        private Quaternion _targetRot;
        private float _targetY;
        private float _moveRateMultiplier = 1f;
        private float startY;

        private void Awake()
        {
            startY = transform.position.y;
            _targetY = startY;
            Vector3 left = transform.position - new Vector3(6f, 0f, 0f);
            Vector3 right = transform.position + new Vector3(6f, 0f, 0f);
            _goals = new Vector3[] { left, right };
        }

        private void Update()
        {
            MoveToGoal();
            RotateToGoal();
        }

        private void OnEnable()
        {
            if (FancyStrafe)
                StartCoroutine(__ShakeItUp());
        }

        private IEnumerator __ShakeItUp()
        {
            while (true)
            {
                if (RandomBool())
                    _targetY = Random.Range(1.5f, 4f);
                else
                    _targetY = startY;

                yield return new WaitForSeconds(1.5f);

                if (RandomBool())
                    _targetRot = Quaternion.Euler(new Vector3(Random.Range(-10f, 10f), Random.Range(-25f, 25f), Random.Range(-50f, 50f)));

                yield return new WaitForSeconds(1.5f);

                _moveRateMultiplier = Random.Range(0.25f, 2.5f);

                yield return new WaitForSeconds(1.5f);
            }
        }

        private bool RandomBool()
        {
            return (Random.Range(0f, 1f) > 0.5f);
        }

        private void RotateToGoal()
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, _targetRot, RotateSpeed * Time.deltaTime);
        }

        private void MoveToGoal()
        {
            Vector3 goal = (_moveRight) ? _goals[1] : _goals[0];
            goal += new Vector3(0f, _targetY, 0f);
            transform.position = Vector3.MoveTowards(transform.position, goal, MoveRate * _moveRateMultiplier * Time.deltaTime);

            if (transform.position == goal)
                _moveRight = !_moveRight;
        }

    }


}
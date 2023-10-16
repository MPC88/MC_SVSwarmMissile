using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MC_SVSwarmMissile
{
    internal class SwarmMissileController : MonoBehaviour
    {
        public enum State { WaitMainLaunchComplete, WaitPod1LaunchComplete, WaitPod2LaunchComplete, WaitPod3LaunchComplete, WaitPod4LaunchComplete, WaitPod5LaunchComplete };

        internal MissileStats stats = null;
        internal SpaceShip owner = null;
        internal List<Transform> targets = null;
        internal Rigidbody carrierRb = null;

        private State state = State.WaitMainLaunchComplete;
        private SmallMissileObject[,] smallMissiles;
        private const float animDelay = 3f;
        private const float interPodDelay = 0.25f;
        private float time = 0f;
        private Animator[] animPods = null;

        private void Awake()
        {
            this.animPods = new Animator[5];
            this.smallMissiles = new SmallMissileObject[5, 4];
            Transform missiles = this.gameObject.transform.GetChild(3);
            for (int pod = 0; pod < 5; pod++)
            {
                Transform podTrans = missiles.GetChild(pod);
                this.animPods[pod] = podTrans.GetComponent<Animator>();
                for (int missile = 0; missile < 4; missile++)
                {
                    Transform missileTrans = podTrans.GetChild(missile);
                    this.smallMissiles[pod, missile] = new SmallMissileObject()
                    {
                        go = missileTrans.gameObject,
                        collider = missileTrans.GetComponent<Collider>(),
                        thruster = missileTrans.GetChild(1).gameObject,
                        audio = missileTrans.GetChild(1).GetComponent<AudioSource>()
                    };
                    smallMissiles[pod, missile].go.tag = "Projectile";
                }
            }
        }

        private void Update()
        {
            this.time += Time.deltaTime;

            switch (state)
            {
                case State.WaitMainLaunchComplete:                    
                    if (this.time >= animDelay)
                    {
                        // Slight force to door 1 (left)
                        Transform door1 = this.transform.GetChild(1);
                        door1.gameObject.tag = "Communication";
                        door1.GetComponent<Collider>().enabled = true;                        
                        door1.GetComponent<Rigidbody>().AddForceAtPosition(Quaternion.Euler(0, -90, 0) * door1.forward * 10, door1.position + door1.forward);
                        // Slight force to door 2 (right)
                        Transform door2 = this.transform.GetChild(2);
                        door2.gameObject.tag = "Communication";
                        door2.GetComponent<Collider>().enabled = true;
                        door1.GetComponent<Rigidbody>().AddForceAtPosition(Quaternion.Euler(0, 90, 0) * door2.forward * 10, door2.position + door2.forward);
                        
                        // Stop main thruster                        
                        this.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>().Stop();

                        // Trigger pod 1 launch
                        this.animPods[0].SetBool("play", true);

                        // Update state
                        this.time = 0;
                        this.state = State.WaitPod1LaunchComplete;
                    }
                    break;
                case State.WaitPod1LaunchComplete:
                    if (this.time > interPodDelay && this.animPods[0].GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
                    {
                        this.animPods[0].enabled = false;
                        this.StartCoroutine(nameof(FirePodMissiles), 0);
                        this.animPods[1].SetBool("play", true);

                        // Update state
                        this.time = 0;
                        this.state = State.WaitPod2LaunchComplete;
                    }
                    break;
                case State.WaitPod2LaunchComplete:
                    if (this.time > interPodDelay && this.animPods[1].GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
                    {
                        this.animPods[1].enabled = false;
                        this.StartCoroutine(nameof(FirePodMissiles), 1);
                        this.animPods[2].SetBool("play", true);

                        // Update state
                        this.time = 0;
                        this.state = State.WaitPod3LaunchComplete;
                    }
                    break;
                case State.WaitPod3LaunchComplete:
                    if (this.time > interPodDelay && this.animPods[2].GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
                    {
                        this.animPods[2].enabled = false;
                        this.StartCoroutine(nameof(FirePodMissiles), 2);
                        this.animPods[3].SetBool("play", true);

                        // Update state
                        this.time = 0;
                        this.state = State.WaitPod4LaunchComplete;
                    }
                    break;
                case State.WaitPod4LaunchComplete:
                    if (this.time > interPodDelay && this.animPods[3].GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
                    {
                        this.animPods[3].enabled = false;
                        this.StartCoroutine(nameof(FirePodMissiles), 3);
                        this.animPods[4].SetBool("play", true);

                        // Update state
                        this.time = 0;
                        this.state = State.WaitPod5LaunchComplete;
                    }
                    break;
                case State.WaitPod5LaunchComplete:
                    if (this.time > interPodDelay && this.animPods[4].GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
                    {
                        this.animPods[4].enabled = false;
                        this.StartCoroutine(nameof(FirePodMissiles), 4);

                        // Enable empty main body and door colliders
                        this.transform.GetChild(0).GetComponent<Collider>().enabled = true;                                                
                        Component.Destroy(this);
                    }
                    break;
            }
        }

        private IEnumerator FirePodMissiles(int podIndex)
        {
            smallMissiles[podIndex, 0].Fire(this.stats, this.owner, GetTarget(podIndex, 0), carrierRb.velocity);
            smallMissiles[podIndex, 1].Fire(this.stats, this.owner, GetTarget(podIndex, 1), carrierRb.velocity);
            yield return new WaitForSeconds(0.5f);
            smallMissiles[podIndex, 2].Fire(this.stats, this.owner, GetTarget(podIndex, 2), carrierRb.velocity);
            smallMissiles[podIndex, 3].Fire(this.stats, this.owner, GetTarget(podIndex, 3), carrierRb.velocity);            
            yield break;
        }

        private Transform GetTarget(int podIndex, int missileIndex)
        {
            int targetIndex = (podIndex * 4) + missileIndex;
            while (targetIndex > this.targets.Count - 1)
                targetIndex -= this.targets.Count;

            return this.targets[targetIndex];
        }
    }
}

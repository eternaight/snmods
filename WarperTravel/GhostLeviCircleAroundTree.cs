using UnityEngine;

namespace WarperTravel
{
    public class GhostLeviCircleAroundTree : CreatureAction
    {
        readonly Vector3 center = Plugin.config.subspaceCenter;
        readonly float radius = Plugin.config.subspacePerformanceRadius;
        const float period = 40;
        const float upndown_period = 95;
        const float upndown_amp = 0;
        float start_time;

        private static int number_of_performers;
        private int my_performer_index;

        public override void StartPerform(Creature creature, float time)
        {
            start_time = time;

            my_performer_index = number_of_performers;
            number_of_performers++;
        }

        public override void StopPerform(Creature creature, float time)
        {
            number_of_performers--;
        }

        private void Update()
        {
            evaluatePriority = Player.main.biomeString == Plugin.config.warperBiome ? float.PositiveInfinity : float.NegativeInfinity;
        }

        public override void Perform(Creature creature, float time, float deltaTime)
        {
            var radius_now = (center - transform.position).magnitude;
            // var radius_now = start_offset.magnitude
            var target = GetTargetPosition(time);
            var distance_to_target = Vector3.Distance(target, transform.position);
            var catchup_factor = 1.5f * (0.63f * Mathf.Atan(distance_to_target - 16) + 1) + 1;
            float desired_velocity = radius_now * 2 * Mathf.PI / period * catchup_factor;

            this.swimBehaviour.SwimTo(target, desired_velocity);
        }

        private Vector3 GetTargetPosition(float time)
        {
            var my_phase_degrees = (float)my_performer_index / number_of_performers * 360f; 
            
            var degrees = (time - start_time) / period * 360 + my_phase_degrees;
            // var upndown = Vector3.up * upndown_amp * Mathf.Sin(time / upndown_period * 2 * Mathf.PI);
            return Quaternion.Euler(0, degrees, 0) * Vector3.forward * radius + center;
        }
    }
}
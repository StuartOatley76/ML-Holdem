using UnityEngine;

namespace Utils {

    /// <summary>
    /// class to turn input into a direction vector for 2d movement
    /// </summary>
    public class MovementController2D : UnitySingleton<MovementController2D> {
        [SerializeField] private KeyCode[] up;
        [SerializeField] private KeyCode[] down;
        [SerializeField] private KeyCode[] left;
        [SerializeField] private KeyCode[] right;

        /// <summary>
        /// Converts any 2d direction input into a Vector3
        /// </summary>
        /// <returns>A Vector 3 with values of either 0 or 1 on the x and z axis</returns>
        public Vector3 CheckForMovement() {
            Vector3 movement = Vector3.zero;
            movement.x = CheckDirection(ref right) ? movement.x + 1 : movement.x;
            movement.x = CheckDirection(ref left) ? movement.x - 1 : movement.x;
            movement.z = CheckDirection(ref up) ? movement.z + 1 : movement.z;
            movement.z = CheckDirection(ref down) ? movement.z - 1 : movement.z;
            return movement;
        }

        /// <summary>
        /// checks whether any of the movement keys for a direction have been pressed
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private bool CheckDirection(ref KeyCode[] direction) {
            for (int i = 0; i < direction.Length; i++) {
                if (Input.GetKey(direction[i])) {
                    return true;
                }
            }
            return false;
        }
    }
}

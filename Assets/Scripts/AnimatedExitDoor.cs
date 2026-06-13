// using System.Collections;
// using UnityEngine;

// [RequireComponent(typeof(Collider2D))]
// public class AnimatedExitDoor : MonoBehaviour
// {
//     [Header("Door Animation")]
//     [SerializeField] private float openAngle = 90f;
//     [SerializeField] private float openSpeed = 180f;

//     [Header("Feedback")]
//     [SerializeField] private AudioClip openSound;

//     private bool isOpen;
//     private Quaternion openedRotation;
//     private Collider2D doorCollider;

//     private void Awake()
//     {
//         openedRotation =
//             Quaternion.Euler(
//                 transform.eulerAngles.x,
//                 transform.eulerAngles.y,
//                 transform.eulerAngles.z + openAngle);

//         doorCollider = GetComponent<Collider2D>();
//     }

//     public void OpenDoor()
//     {
//         if (isOpen)
//             return;

//         isOpen = true;

//         if (openSound != null)
//         {
//             AudioSource.PlayClipAtPoint(
//                 openSound,
//                 transform.position);
//         }

//         StartCoroutine(OpenDoorRoutine());
//     }

//     private IEnumerator OpenDoorRoutine()
//     {
//         while (Quaternion.Angle(transform.rotation, openedRotation) > 1f)
//         {
//             transform.rotation =
//                 Quaternion.RotateTowards(
//                     transform.rotation,
//                     openedRotation,
//                     openSpeed * Time.deltaTime);

//             yield return null;
//         }

//         transform.rotation = openedRotation;

//         if (doorCollider != null)
//             doorCollider.enabled = false;
//     }
// }


using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AnimatedExitDoor : MonoBehaviour
{
    [Header("Door Animation")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 180f;

    [Header("Feedback")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioSource audioSource;

    private bool isOpen;
    private Quaternion openedRotation;
    private Collider2D doorCollider;

    private void Awake()
    {
        openedRotation =
            Quaternion.Euler(
                transform.eulerAngles.x,
                transform.eulerAngles.y,
                transform.eulerAngles.z + openAngle);

        doorCollider = GetComponent<Collider2D>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void OpenDoor()
    {
        if (isOpen)
            return;

        isOpen = true;

        PlayOpenSound();

        StartCoroutine(OpenDoorRoutine());
    }

    private void PlayOpenSound()
    {
        if (openSound == null)
            return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(openSound);
        }
        else
        {
            AudioSource.PlayClipAtPoint(
                openSound,
                transform.position);
        }
    }

    private IEnumerator OpenDoorRoutine()
    {
        while (Quaternion.Angle(transform.rotation, openedRotation) > 1f)
        {
            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    openedRotation,
                    openSpeed * Time.deltaTime);

            yield return null;
        }

        transform.rotation = openedRotation;

        if (doorCollider != null)
            doorCollider.enabled = false;
    }
}
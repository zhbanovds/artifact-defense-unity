using UnityEngine;

public static class CombatTargeting
{
    public static bool TryGetTarget<T>(Collider collider, out T target) where T : class
    {
        target = collider.GetComponent(typeof(T)) as T;

        if (target != null)
        {
            return true;
        }

        target = collider.GetComponentInParent(typeof(T)) as T;
        return target != null;
    }
}

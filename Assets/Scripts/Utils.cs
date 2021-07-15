// This class contains some helper functions.
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Security.Cryptography;
using System.Reflection;

public class Utils
{
    // Mathf.Clamp only works for float and int. we need some more versions:
    public static long Clamp(long value, long min, long max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    // is any of the keys UP?
    public static bool AnyKeyUp(KeyCode[] keys)
    {
        // avoid Linq.Any because it is HEAVY(!) on GC and performance
        foreach (KeyCode key in keys)
            if (Input.GetKeyUp(key))
                return true;
        return false;
    }

    // is any of the keys DOWN?
    public static bool AnyKeyDown(KeyCode[] keys)
    {
        // avoid Linq.Any because it is HEAVY(!) on GC and performance
        foreach (KeyCode key in keys)
            if (Input.GetKeyDown(key))
                return true;
        return false;
    }

    // is any of the keys PRESSED?
    public static bool AnyKeyPressed(KeyCode[] keys)
    {
        // avoid Linq.Any because it is HEAVY(!) on GC and performance
        foreach (KeyCode key in keys)
            if (Input.GetKey(key))
                return true;
        return false;
    }

    // Distance between two ClosestPointOnBounds
    // this is needed in cases where entites are really big. in those cases,
    // we can't just move to entity.transform.position, because it will be
    // unreachable. instead we have to go the closest point on the boundary.
    //
    // Vector2.Distance(a.transform.position, b.transform.position):
    //    _____        _____
    //   |     |      |     |
    //   |  x==|======|==x  |
    //   |_____|      |_____|
    //
    //
    // Utils.ClosestDistance(a.collider, b.collider):
    //    _____        _____
    //   |     |      |     |
    //   |     |x====x|     |
    //   |_____|      |_____|
    //
    public static float ClosestDistance(Collider2D a, Collider2D b)
    {
        return Vector2.Distance(a.ClosestPointOnBounds(b.transform.position),
                                b.ClosestPointOnBounds(a.transform.position));
    }

    // ClosestDistance version for Entities for consistency with uMMORPG 3D
    public static float ClosestDistance(Entity a, Entity b)
    {
        return ClosestDistance(a.collider, b.collider);
    }

    // closest point from an entity's collider to another point
    // this is used all over the place, so let's put it into one place so it's
    // easier to modify the method if needed
    public static Vector2 ClosestPoint(Entity entity, Vector2 point)
    {
        return entity.collider.ClosestPointOnBounds(point);
    }

    // helper function to find the nearest Transform from a point 'from'
    // => players can respawn frequently, and the game could have many start
    //    positions so this function does matter even if not in hot path.
    public static Transform GetNearestTransform(List<Transform> transforms, Vector2 from)
    {
        // note: avoid Linq for performance / GC
        Transform nearest = null;
        foreach (Transform tf in transforms)
        {
            // better candidate if we have no candidate yet, or if closer
            if (nearest == null ||
                Vector2.Distance(tf.position, from) < Vector2.Distance(nearest.position, from))
                nearest = tf;
        }
        return nearest;
    }

    // parse last upper cased noun from a string, e.g.
    //   EquipmentWeaponBow => Bow
    //   EquipmentShield => Shield
    static Regex lastNountRegEx = new Regex(@"([A-Z][a-z]*)"); // cache to avoid allocations. this is used a lot.
    public static string ParseLastNoun(string text)
    {
        MatchCollection matches = lastNountRegEx.Matches(text);
        return matches.Count > 0 ? matches[matches.Count-1].Value : "";
    }

    // check if the cursor is over a UI or OnGUI element right now
    // note: for UI, this only works if the UI's CanvasGroup blocks Raycasts
    // note: for OnGUI: hotControl is only set while clicking, not while zooming
    public static bool IsCursorOverUserInterface()
    {
        // IsPointerOverGameObject check for left mouse (default)
        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        // IsPointerOverGameObject check for touches
        for (int i = 0; i < Input.touchCount; ++i)
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;

        // OnGUI check
        return GUIUtility.hotControl != 0;
    }
    
    // PBKDF2 hashing recommended by NIST:
    // http://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-132.pdf
    // salt should be at least 128 bits = 16 bytes
    public static string PBKDF2Hash(string text, string salt)
    {
        byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
        Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(text, saltBytes, 10000);
        byte[] hash = pbkdf2.GetBytes(20);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    public static float RandomFloatBySeed(float min, float max, double seed)
    {
        //Debug.Log(seed);
        UnityEngine.Random.InitState((int)seed);
        float result = UnityEngine.Random.Range(min, max);
        UnityEngine.Random.InitState((int)DateTime.Now.Ticks);//set to default
        return result;
    }
}

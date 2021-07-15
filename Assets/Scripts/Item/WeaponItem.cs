using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Loop Item/Weapon", order=999)]
public class WeaponItem : EquipmentItem
{
    public ShootPattern shootPattern;
    public ProjectileAttribute projectileAttribute;
}

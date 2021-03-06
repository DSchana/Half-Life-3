﻿using Artemis.Engine.Multiforms;
using System;
using System.Collections.Generic;
using Half_Life_3.Entities.Weapons;
using Half_Life_3.Entities.Characters;
using Half_Life_3.Entities.Obstacles;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Text;

namespace Half_Life_3.Entities
{
    /// <summary>
    /// Stores all characters and determines how damage is dealt.
    /// Used to allow weapons to be hitscan rather than projectiles.
    /// </summary>
    class EntityManager
    {
        /// <summary>
        /// List of Characters to handle
        /// </summary>
        public Dictionary<string, Entity> Entities { get; private set; }

        /// <summary>
        /// Position of camera relative to world
        /// </summary>
        public Vector2 CameraPosition { get; private set; }

        public EntityManager()
        {
            Entities = new Dictionary<string, Entity>();
        }

        public void Add(Entity entity)
        {
            Entities.Add(entity.Name, entity);
        }

        // Overload Add method for other classes

        public void Kill(string name)
        {
            try
            {
                if (Entities[name].Type == EntityType.PlayableCharacter)
                {
                    (Entities[name] as PlayableCharacter).StopUpdating();
                }

                Entities.Remove(name);
            }
            catch (KeyNotFoundException e)
            {
                throw new KeyNotFoundException(String.Format("'{0}' not a character. \nError at: \n'{1}'", name));
            }
        }

        public void KillAll()
        {
            List<string> ToKill = new List<string>();

            foreach (KeyValuePair<string, Entity> entity in Entities)
            {
                ToKill.Add(entity.Key);
            }

            foreach (string kill in ToKill)
            {
                Kill(kill);
            }
        }

        /// <summary>
        /// Allow manager to determine damage type and perform appropriate actions
        /// </summary>
        public void DealDamage()
        {
            foreach (KeyValuePair<string, Entity> entity in Entities)
            {
                if (entity.Value.Type == EntityType.Character)
                {
                    Character character = entity.Value as Character;
                    if (character.CurrentWeapon.TypeDamage == DamageType.Hitscan)
                    {
                        ScanHit(character);
                    }
                    else if (character.CurrentWeapon.TypeDamage == DamageType.Melee)
                    {
                        MeeleHit(character);
                    }
                }
                else if (entity.Value.Type == EntityType.Explosive)
                {
                    Explosive explosive = entity.Value as Explosive;
                    ExplosiveDamage(explosive);
                }
            }
        }

        /// <summary>
        /// Allow manager to determine damage type and perform appropriate actions
        /// </summary>
        /// <param name="character">The character wielding the weapon</param>
        public void DealDamage(Entity entity)
        {
            if (entity.Type == EntityType.Character || entity.Type == EntityType.CombineSoldier || entity.Type == EntityType.PlayableCharacter)
            {
                Character character = entity as Character;
                if (character.CurrentWeapon.TypeDamage == DamageType.Hitscan)
                {
                    ScanHit(character);
                }
                else if (character.CurrentWeapon.TypeDamage == DamageType.Melee)
                {
                    MeeleHit(character);
                }
            }
            else if (entity.Type == EntityType.Explosive)
            {
                Explosive explosive = entity as Explosive;
                ExplosiveDamage(explosive);
            }
        }

        /// <summary>
        /// Allow manager to determine damage type and perform appropriate actions
        /// </summary>
        /// <param name="damageType">The type of damage a character's weapon will be forced to exert</param>
        public void DealDamage(DamageType damageType)
        {
            foreach (KeyValuePair<string, Entity> entity in Entities)
            {
                if (entity.Value.Type == EntityType.Character)
                {
                    Character character = entity.Value as Character;
                    if (damageType == DamageType.Hitscan)
                    {
                        ScanHit(character);
                    }
                    else if (damageType == DamageType.Melee)
                    {
                        MeeleHit(character);
                    }
                }
                else if (entity.Value.Type == EntityType.Explosive && damageType == DamageType.Projectile)
                {
                    Explosive explosive = entity.Value as Explosive;
                    ExplosiveDamage(explosive);
                }
            }
        }

        /// <summary>
        /// Allow manager to determine damage type and perform appropriate actions
        /// </summary>
        /// <param name="character">The character wielding the weapon</param>
        /// <param name="damageType">The type of damage a character's weapon will be forced to exert</param>
        public void DealDamage(Entity entity, DamageType damageType)
        {
            if (entity.Type == EntityType.Character)
            {
                Character character = entity as Character;
                if (damageType == DamageType.Hitscan)
                {
                    ScanHit(character);
                }
                else if (damageType == DamageType.Melee)
                {
                    MeeleHit(character);
                }
            }
            else if (entity.Type == EntityType.Explosive && damageType == DamageType.Projectile)
            {
                Explosive explosive = entity as Explosive;
                ExplosiveDamage(explosive);
            }
        }

        /// <summary>
        /// Allow manager to deal damage from hitscan weapons
        /// </summary>
        public void ScanHit(Character character)
        {
            double slope = Math.Sin(character.Rotation) / Math.Cos(character.Rotation);
            double y_int = character.WorldPosition.Y - (slope * character.WorldPosition.X);
            Entity actualTarget = null;
            double actualDistanceToTarget = double.MaxValue;

            // Check collision and find target
            foreach (KeyValuePair<string, Entity> potentialTarget in Entities)
            {
                if (Entities[character.Name] == potentialTarget.Value)
                {
                    continue;
                }

                // Case 1: Line collides with left side of BoundingBox
                // case 2: Line collides with left side of BoundingBox. Used if case 1 is false
                if ((slope * potentialTarget.Value.BoundingBox.X) + y_int - potentialTarget.Value.BoundingBox.Y <= potentialTarget.Value.BoundingBox.Height ||
                    (slope * (potentialTarget.Value.BoundingBox.X + potentialTarget.Value.BoundingBox.Width)) + y_int - potentialTarget.Value.BoundingBox.Y <= potentialTarget.Value.BoundingBox.Height)
                {
                    if (actualTarget == null)
                    {
                        actualTarget = potentialTarget.Value;
                        actualDistanceToTarget = Math.Sqrt(Math.Pow(Math.Abs(potentialTarget.Value.WorldPosition.X - character.WorldPosition.X), 2) + Math.Pow(Math.Abs(potentialTarget.Value.WorldPosition.Y - character.WorldPosition.Y), 2));
                        continue;
                    }

                    // Find closet target. This is so if there are many targets in a line, only the closest is hit
                    double potentialDistanceToTarget = Math.Sqrt(Math.Pow(Math.Abs(potentialTarget.Value.WorldPosition.X - character.WorldPosition.X), 2) + Math.Pow(Math.Abs(potentialTarget.Value.WorldPosition.Y - character.WorldPosition.Y), 2));

                    if (potentialDistanceToTarget < actualDistanceToTarget)
                    {
                        actualTarget = potentialTarget.Value;
                        actualDistanceToTarget = potentialDistanceToTarget;
                    }
                }
            }

            // true if target is in front of character and not behind
            if (actualTarget != null && Math.Abs(character.WorldPosition.X + Math.Cos(character.Rotation) - actualTarget.WorldPosition.X) < Math.Abs(character.WorldPosition.X - actualTarget.WorldPosition.X))
            {
                if (actualDistanceToTarget <= (int)character.CurrentWeapon.Range)
                {
                    actualTarget.TakeDamage(character.CurrentWeapon.RangeDamage);
                }
                else
                {
                    actualTarget.TakeDamage(character.CurrentWeapon.RangeDamage / 2);  // Damage drop-off if target is too far
                }
            }
        }

        public void MeeleHit(Character character)
        {
            double slope = (float)(Math.Sin(character.Rotation) / Math.Cos(character.Rotation));
            double y_int = character.WorldPosition.Y - (slope * character.WorldPosition.X);
            List<Entity> actualTargets = new List<Entity>();

            foreach (KeyValuePair<string, Entity> potentialTarget in Entities)
            {
                try
                {
                    if (Entities[character.Name] == potentialTarget.Value)
                    {
                        continue;
                    }
                }
                catch (Exception e)
                {
                    continue;
                }

                // Case 1: Line collides with left side of BoundingBox
                // case 2: Line collides with left side of BoundingBox. Used if case 1 is false
                if ((slope * potentialTarget.Value.BoundingBox.X) + y_int - potentialTarget.Value.BoundingBox.Y <= potentialTarget.Value.BoundingBox.Height ||
                    (slope * (potentialTarget.Value.BoundingBox.X + potentialTarget.Value.BoundingBox.Width)) + y_int - potentialTarget.Value.BoundingBox.Y <= potentialTarget.Value.BoundingBox.Height)
                {
                    double potentialDistanceToTarget = Math.Sqrt(Math.Pow(Math.Abs(potentialTarget.Value.WorldPosition.X - character.WorldPosition.X), 2) + Math.Pow(Math.Abs(potentialTarget.Value.WorldPosition.Y - character.WorldPosition.Y), 2));

                    if (potentialDistanceToTarget < (int)character.CurrentWeapon.MeleeRange)
                    {
                        actualTargets.Add(potentialTarget.Value);
                    }
                }
            }

            foreach (var target in actualTargets)
            {
                // true if target is in front of character and not behind
                if (Math.Abs(character.WorldPosition.X + Math.Cos(character.Rotation) - target.WorldPosition.X) < Math.Abs(character.WorldPosition.X - target.WorldPosition.X))
                {
                    target.TakeDamage(character.CurrentWeapon.MeleeDamage);
                }
            }
        }
        
        public void ExplosiveDamage(Explosive explosive)
        {
            foreach (KeyValuePair<string, Entity> entity in Entities)
            {
                if (entity.Value == explosive)
                {
                    continue;
                }

                double distance = Math.Sqrt(Math.Pow(Math.Abs(entity.Value.WorldPosition.X - explosive.WorldPosition.X), 2) + Math.Pow(Math.Abs(entity.Value.WorldPosition.Y - explosive.WorldPosition.Y), 2));

                if (distance <= 50)
                {
                    entity.Value.TakeDamage(explosive.ExplosiveDamage);
                }
                else if (distance <= 100)
                {
                    entity.Value.TakeDamage(explosive.ExplosiveDamage / 2);
                }
                else if (distance <= 400)
                {
                    entity.Value.TakeDamage(explosive.ExplosiveDamage / 4);
                }
                else if (distance <= 800)
                {
                    entity.Value.TakeDamage(explosive.ExplosiveDamage / 8);
                }
            }
        }

        public bool IsCollisionFree(Entity entity)
        {
            foreach (KeyValuePair<string, Entity> collisionTarget in Entities)
            {
                if (collisionTarget.Value == entity)
                {
                    continue;
                }

                if (entity.BoundingBox.Contains(collisionTarget.Value.BoundingBox))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check collisions with other entities
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="entityBoundingBox"></param>
        /// <returns>
        /// returns 0, 1, 2, 3 if
        /// no collision, Top collision, Side collision, Both collisions
        /// </returns>
        public int IsCollisionFree(Entity entity, Rectangle entityBoundingBox)
        {
            List<int> intersections = new List<int>();

            foreach (KeyValuePair<string, Entity> collisionTarget in Entities)
            {
                if (collisionTarget.Value == entity)
                {
                    continue;
                }

                intersections.Add(CheckIntersection(entityBoundingBox, collisionTarget.Value.BoundingBox));
            }

            if (intersections.Contains(1) && intersections.Contains(2))
            {
                return 3;
            }
            else if (intersections.Contains(1))
            {
                return 1;
            }
            else if (intersections.Contains(2))
            {
                return 2;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Check intersection of two Rectangles
        /// </summary>
        /// <param name="rect1"></param>
        /// <param name="rect2"></param>
        /// <returns>
        /// returns 0, 1, 2 if
        /// no collision, Top collision, Side collision
        /// </returns>
        public int CheckIntersection(Rectangle rect1, Rectangle rect2)
        {
            if (!rect1.Intersects(rect2))
            {
                return 0;
            }
            else
            {
                if (Math.Abs(rect1.Width - Math.Abs(rect1.X - rect2.X)) >= Math.Abs(rect1.Height - Math.Abs(rect1.Y - rect2.Y)))
                {
                    return 1;
                }
                if (Math.Abs(rect1.Width - Math.Abs(rect1.X - rect2.X)) < Math.Abs(rect1.Height - Math.Abs(rect1.Y - rect2.Y)))
                {
                    return 2;
                }
            }
            return 0;  // Should never reach here
        }

        public void Update()
        {
            if (Game1.StoryManager.Flags["helicopter"].IsActive)
            {
                CameraPosition = Game1.Jim.WorldPosition - Game1.Freeman.ScreenPosition;
            }
            else
            {
                CameraPosition = Game1.Freeman.WorldPosition - Game1.Freeman.ScreenPosition;
            }

            foreach (var entity in Entities.Values)
            {
                if (entity.Sprites != null)
                {
                    entity.Sprites.Update();
                }
            }
        }

        public void Render()
        {
            foreach (var entity in Entities.Values)
            {
                if (entity.Type == EntityType.Helicopter)
                {
                    Helicopter hell = entity as Helicopter;
                    hell.Draw();
                }
                else
                {
                    entity.Show();
                }
            }
        }
    }
}

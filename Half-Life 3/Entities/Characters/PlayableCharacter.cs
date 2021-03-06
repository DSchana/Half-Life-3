﻿using Artemis.Engine;
using Artemis.Engine.Input;
using Half_Life_3.Entities.Weapons;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Half_Life_3.Entities.Characters
{
    class PlayableCharacter : Character
    {
        /// <summary>
        /// List of weapons availible to player
        /// </summary>
        public List<Weapon> Weapons { get; private set; }

        /// <summary>
        /// Previous weapon selected
        /// </summary>
        public Weapon PreviousWeapon { get; private set; }

        public PlayableCharacter(string name, int x, int y) : base(name, x, y)
        {
            Console.WriteLine("\nMAKING FREEMAN");
            ScreenPosition = new Vector2(ArtemisEngine.DisplayManager.WindowResolution.Width / 2, ArtemisEngine.DisplayManager.WindowResolution.Height / 2);

            WorldPosition = new Vector2(x, y);

            Weapons = new List<Weapon>();

            Weapons.Add(new Weapon("USPMatch", this, WeaponType.USPMatch));
            Weapons.Add(new Weapon("MP7", this, WeaponType.MP7));
            Weapons.Add(new Weapon("SPAS12", this, WeaponType.SPAS12));
            Weapons.Add(new Weapon("Knife", this, WeaponType.Knife));  // Probably take this away from dr.freeman

            CurrentWeapon = Weapons[0];
            CurrentWeapon.IsActive = true;

            PreviousWeapon = Weapons[1];

            Type = EntityType.PlayableCharacter;
            IsPlayable = true;

            SetMaxHealth(100);

            Sprites = new Sprite();
            Sprites.ToggleAlwaysAnimate();

            Sprites.LoadDirectory(@"Content\Resources\Gordon Freeman\Knife");
            Sprites.LoadDirectory(@"Content\Resources\Gordon Freeman\MP7");
            Sprites.LoadDirectory(@"Content\Resources\Gordon Freeman\SPAS12");
            Sprites.LoadDirectory(@"Content\Resources\Gordon Freeman\USPMatch");

            ChangeState("idle");

            AddUpdater(UpdateWeapon);
            AddUpdater(Rotate);
            AddUpdater(Move);
            AddUpdater(Attack);

            Console.WriteLine("MADE FREEMAN\n");
        }

        public void StopUpdating()
        {
            RemoveUpdater(UpdateWeapon);
            RemoveUpdater(Rotate);
            RemoveUpdater(Move);
            RemoveUpdater(Attack);
        }

        public void UpdateWeapon()
        {
            if (ArtemisEngine.Keyboard.IsClicked(Keys.D1))      // UPSMatch
            {
                ChangeWeapon(0);
            }
            else if (ArtemisEngine.Keyboard.IsClicked(Keys.D2))  // MP7
            {
                ChangeWeapon(1);
            }
            else if (ArtemisEngine.Keyboard.IsClicked(Keys.D3))  // SPAS12
            {
                ChangeWeapon(2);
            }
            else if (ArtemisEngine.Keyboard.IsClicked(Keys.D4))  //  Knife
            {
                ChangeWeapon(3);
            }
            else if (ArtemisEngine.Keyboard.IsClicked(Keys.D5))  // Nothing Yet
            {
                ChangeWeapon(4);
            }
            else if (ArtemisEngine.Keyboard.IsClicked(Keys.Q))
            {
                CurrentWeapon.IsActive = false;
                Weapon middleWeapon = PreviousWeapon;
                PreviousWeapon = CurrentWeapon;
                CurrentWeapon = middleWeapon;
                CurrentWeapon.IsActive = true;
                ChangeState("idle");
            }
        }

        public void ChangeWeapon(int weaponSlot)
        {
            if (weaponSlot < Weapons.Count && !Sprites.Attacking && CurrentWeapon != Weapons[weaponSlot])
            {
                CurrentWeapon.IsActive = false;
                PreviousWeapon = CurrentWeapon;
                CurrentWeapon = Weapons[weaponSlot];
                CurrentWeapon.IsActive = true;
                ChangeState("idle");
            }
        }

        private void Rotate()
        {
            Vector2 direction = ArtemisEngine.Mouse.PositionVector - ScreenPosition;
            direction.Normalize();

            Rotation = (float)Math.Atan2(direction.Y, direction.X);
        }

        private void Move()
        {
            bool isMoving = false;
            Vector2 NewWorldPosition = new Vector2(WorldPosition.X, WorldPosition.Y);

            if (ArtemisEngine.Keyboard.IsHeld(Keys.W))
            {
                isMoving = true;
                NewWorldPosition.X += (float)(Speed * Math.Cos(Rotation));
                NewWorldPosition.Y += (float)(Speed * Math.Sin(Rotation));
            }
            if (ArtemisEngine.Keyboard.IsHeld(Keys.A))
            {
                isMoving = true;
                NewWorldPosition.X -= (float)(Speed * Math.Cos(Rotation + 90));
                NewWorldPosition.Y -= (float)(Speed * Math.Sin(Rotation + 90));
            }
            if (ArtemisEngine.Keyboard.IsHeld(Keys.S))
            {
                isMoving = true;
                NewWorldPosition.X -= (float)(Speed * Math.Cos(Rotation));
                NewWorldPosition.Y -= (float)(Speed * Math.Sin(Rotation));
            }
            if (ArtemisEngine.Keyboard.IsHeld(Keys.D))
            {
                isMoving = true;
                NewWorldPosition.X += (float)(Speed * Math.Cos(Rotation + 90));
                NewWorldPosition.Y += (float)(Speed * Math.Sin(Rotation + 90));
            }
            if (ArtemisEngine.Keyboard.IsHeld(Keys.LeftShift))
            {
                Speed = 10;
            }
            else
            {
                Speed = 5;
            }

            if (isMoving && !Sprites.CurrentState.Contains("move"))
            {
                ChangeState("move");
            }
            else if (!isMoving && !Sprites.CurrentState.Contains("idle"))
            {
                ChangeState("idle");
            }

            int collisionsValue = Game1.EntityManager.IsCollisionFree(this, new Rectangle((int)NewWorldPosition.X, (int)NewWorldPosition.Y, BoundingBox.Width, BoundingBox.Height));
            if (collisionsValue == 0)
            {
                WorldPosition = NewWorldPosition;
            }
            else if (collisionsValue == 1)
            {
                WorldPosition = new Vector2(NewWorldPosition.X, WorldPosition.Y);
            }
            else if (collisionsValue == 2)
            {
                WorldPosition = new Vector2(WorldPosition.X, NewWorldPosition.Y);
            }
        }

        private void Attack()
        {
            if (ArtemisEngine.Keyboard.IsClicked(Keys.R) && CurrentWeapon.TypeWeapon != WeaponType.Knife || CurrentWeapon.ClipAmmo <= 0 && CurrentWeapon.TypeWeapon != WeaponType.Knife)
            {
                ChangeState("reload", true);
                CurrentWeapon.Reload();
            }
            else if (ArtemisEngine.Mouse.IsClicked(MouseButton.Left) && CurrentWeapon.ClipAmmo > 0 && !Sprites.CurrentState.Contains("shoot") && CurrentWeapon.TypeWeapon != WeaponType.Knife )
            {
                ChangeState("shoot");
                CurrentWeapon.Fire();
            }
            else if (ArtemisEngine.Mouse.IsClicked(MouseButton.Right) && !Sprites.CurrentState.Contains("meleeattack"))
            {
                ChangeState("meleeattack");
                CurrentWeapon.Fire(DamageType.Melee);
            }
        }
    }
}
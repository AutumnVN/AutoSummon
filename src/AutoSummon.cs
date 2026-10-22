using System;
using Terraria;
using Terraria.Audio;
using TerrariaModder.Core;
using TerrariaModder.Core.Config;
using TerrariaModder.Core.Events;
using TerrariaModder.Core.Logging;

namespace AutoSummon
{
    public class AutoSummon : IMod, IModLifecycle
    {
        public string Id => "auto-summon";
        public string Name => "Auto Summon";
        public string Version => "1.0.0";

        private ILogger _log;
        private AutoSummonConfig _config;

        private static readonly Random _random = new Random();

        private MinionColumnItems _minionColumnItems;
        private int _previousMinionColumnIndex;
        private int _previousMinionCap;

        private bool _hasSpawned;
        private bool _isUsingMinionSlotItem;
        private bool _hasUsedMinionSlotItem;

        private bool _hasChangedMinionCap;
        private bool _hasChangedMinionSlotItem;

        private bool _shouldSummonMinions;

        public void Initialize(ModContext context)
        {
            _log = context.Logger;
            _config = context.GetConfig<AutoSummonConfig>();

            _minionColumnItems = new MinionColumnItems();

            FrameEvents.OnPreUpdate += OnPreUpdate;
            FrameEvents.OnPostUpdate += OnPostUpdate;
            PlayerEvents.OnPlayerSpawn += OnPlayerSpawn;
            GameEvents.OnWorldLoad += OnWorldLoad;
            GameEvents.OnWorldUnload += OnWorldUnload;

            ResetRuntimeState();
            _log.Info($"{Name} v{Version} initialized");
        }

        public void OnContentReady(ModContext context)
        {
        }

        public void Unload()
        {
            FrameEvents.OnPreUpdate -= OnPreUpdate;
            FrameEvents.OnPostUpdate -= OnPostUpdate;
            PlayerEvents.OnPlayerSpawn -= OnPlayerSpawn;
            GameEvents.OnWorldLoad -= OnWorldLoad;
            GameEvents.OnWorldUnload -= OnWorldUnload;

            ResetRuntimeState();
            _log.Info($"{Name} unloaded");
        }

        public void OnWorldLoad()
        {
            ResetRuntimeState();
        }

        public void OnWorldUnload()
        {
            ResetRuntimeState();
        }

        private int GetConfigMinionColumnIndex()
        {
            return _config.MinionColumnIndex - 1;
        }

        private void OnPlayerSpawn(PlayerSpawnEventArgs args)
        {
            if (Main.dedServ || args.PlayerIndex != Main.myPlayer) return;

            _hasSpawned = true;
        }

        private void OnPreUpdate()
        {
            if (Main.dedServ) return;

            Player player = Main.player[Main.myPlayer];
            if (player == null || !player.active) return;

            if (_minionColumnItems.CheckItemChange(_previousMinionColumnIndex))
            {
                _hasChangedMinionSlotItem = true;
            }

            if (!player.mouseInterface && Main.mouseItem.IsAir &&
                player.selectedItem == _previousMinionColumnIndex &&
                MinionUtil.CheckItemSummonsMinions(player.HeldItem) &&
                player.controlUseItem)
            {
                if (!_isUsingMinionSlotItem)
                {
                    _isUsingMinionSlotItem = true;
                    _hasUsedMinionSlotItem = true;

                    player.itemAnimation = 0;
                }
            }
            else
            {
                _isUsingMinionSlotItem = false;
            }
        }

        private void OnPostUpdate()
        {
            if (Main.dedServ) return;

            Player player = Main.player[Main.myPlayer];
            if (player == null || !player.active) return;

            if (_shouldSummonMinions && !player.mouseInterface && Main.mouseItem.IsAir)
            {
                SummonMinions(player);

                _shouldSummonMinions = false;
            }

            if (_previousMinionCap != player.maxMinions)
            {
                _hasChangedMinionCap = true;
            }

            if (_hasSpawned ||
               _hasChangedMinionSlotItem ||
               _hasUsedMinionSlotItem ||
               _hasChangedMinionCap)
            {
                _shouldSummonMinions = true;
                MinionUtil.RemoveAllSummonMinions();

                _hasSpawned = false;
                _hasUsedMinionSlotItem = false;
                _hasChangedMinionSlotItem = false;
                _hasChangedMinionCap = false;
            }

            _previousMinionCap = player.maxMinions;
            _minionColumnItems.UpdateItems(_previousMinionColumnIndex);
            _previousMinionColumnIndex = GetConfigMinionColumnIndex();
        }

        private void SummonMinions(Player player)
        {
            if (player.mouseInterface || !Main.mouseItem.IsAir)
                return;

            int previousSelectedItem = player.selectedItem;

            if (previousSelectedItem < 0 || previousSelectedItem >= 10)
                return;

            Item previousHeldItem = player.inventory[previousSelectedItem];

            for (int i = 0; i < _minionColumnItems.Length; ++i)
            {
                int slot = _previousMinionColumnIndex + i * 10;
                Item summonItem = player.inventory[slot];

                if (!MinionUtil.CheckItemSummonsMinions(summonItem))
                    continue;

                Projectile proj = new Projectile();
                proj.SetDefaults(summonItem.shoot);
                float minionSlotSize = proj.minionSlots != 0 ? proj.minionSlots : 1;

                float minionSlotsUsed = MinionUtil.CountMinionSlotsUsed();

                while (minionSlotsUsed + minionSlotSize <= player.maxMinions)
                {
                    float previousScreenPositionX = Main.screenPosition.X;
                    float previousScreenPositionY = Main.screenPosition.Y;

                    Main.screenPosition.X = player.Center.X - Main.MouseScreen.X + _random.Next(0, 10) * 16 - 5 * 16;
                    Main.screenPosition.Y = player.Center.Y - Main.MouseScreen.Y + _random.Next(0, 10) * 16 - 5 * 16;

                    bool previousControlUseItem = player.controlUseItem;
                    LegacySoundStyle previousUseSound = summonItem.UseSound;
                    int previousMana = summonItem.mana;

                    if (_config.MuteSummonUseSounds)
                        summonItem.UseSound = null;
                    summonItem.mana = 0;

                    player.inventory[previousSelectedItem] = summonItem;

                    player.controlUseItem = true;
                    player.itemAnimation = 0;
                    player.ItemCheck();
                    player.controlUseItem = false;

                    while (player.itemAnimation > 0)
                    {
                        player.ItemCheck();
                    }

                    player.inventory[previousSelectedItem] = previousHeldItem;
                    player.controlUseItem = previousControlUseItem;
                    summonItem.UseSound = previousUseSound;
                    summonItem.mana = previousMana;

                    Main.screenPosition.X = previousScreenPositionX;
                    Main.screenPosition.Y = previousScreenPositionY;

                    minionSlotsUsed += minionSlotSize;
                }
            }
        }

        private void ResetRuntimeState()
        {
            _minionColumnItems = new MinionColumnItems();

            _previousMinionColumnIndex = GetConfigMinionColumnIndex();
            _previousMinionCap = 0;

            _hasSpawned = false;
            _isUsingMinionSlotItem = false;
            _hasUsedMinionSlotItem = false;

            _hasChangedMinionCap = false;
            _hasChangedMinionSlotItem = false;

            _shouldSummonMinions = false;
        }
    }
}

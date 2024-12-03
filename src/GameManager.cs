using Godot;
using Game.Autoload;
using Game.Systems;

namespace Game
{
    public partial class GameManager : Node3D
    {
        private EntityManager _entities;
        private HexGridSystem _hexGridSystem;
        private PathFinderSystem _pathFinderSystem;
        private UnitSystem _unitSystem;
        private TurnSystem _turnSystem;
        private CombatSystem _combatSystem;
        private AnimationSystem _animationSystem;

        public override void _Ready()
        {
            _entities = new EntityManager(this);
            _hexGridSystem = new HexGridSystem(_entities, 5);
            _pathFinderSystem = new PathFinderSystem(_entities, _hexGridSystem);
            _unitSystem = new UnitSystem(_entities);
            _combatSystem = new CombatSystem(_entities, _hexGridSystem);
            _animationSystem = new AnimationSystem();
            _turnSystem = new TurnSystem(_entities);

            var player = _unitSystem.CreatePlayer(new Vector3I(0, 4, -4));
            _unitSystem.CreateGrunt(new Vector3I(-2, 1, 3));
            _unitSystem.CreateGrunt(new Vector3I(2, -3, 1));

            _turnSystem.OnTurnChanged += OnTurnChanged;
            _turnSystem.StartCombat();

            EventBus.Instance.TileSelect += OnTileClicked;
        }

        private async void OnTileClicked(Entity tile)
        {
            var player = _entities.GetPlayer();
            var tileCoord = tile.Get<HexCoordComponent>().HexCoord;
            GD.Print(tileCoord);
            // GD.Print(player.Has<HexCoordComponent>());

            var targetHex = _hexGridSystem.WorldToHex(tileCoord);
            var currentPos = player.Get<HexCoordComponent>().HexCoord;
            var path = _pathFinderSystem.FindPath(currentPos, targetHex);
            GD.Print(path.Count);

            if (path.Count > 0)
            {
                // Move player
                await _animationSystem.MoveEntity(player, path);

                // Check if player would die from this move
                if (_combatSystem.WouldDieFromMove(player, currentPos, targetHex))
                {
                    _turnSystem.RemoveUnit(player);
                    _entities.RemoveEntity(player);
                    GameOver();
                    return;
                }

                // Check for killed enemies
                var killedEnemies = _combatSystem.GetKillableEnemies(player, currentPos, targetHex);
                foreach (var enemy in killedEnemies)
                {
                    _turnSystem.RemoveUnit(enemy);
                    _entities.RemoveEntity(enemy);
                }

                if (_entities.GetEnemies().Count == 0)
                {
                    Victory();
                    return;
                }

                _turnSystem.EndTurn();
            }
        }

        private void OnTurnChanged(Entity unit)
        {
            if (unit.Get<UnitTypeComponent>().UnitType == UnitType.Grunt)
            {
                ProcessEnemyTurn(unit);
            }
        }

        public async void ProcessEnemyTurn(Entity enemy)
        {
            var player = _entities.GetPlayer();
            var playerPos = _hexGridSystem.WorldToHex(player.Get<RenderComponent>().Node3D.Position);
            var currentEnemyPos = _hexGridSystem.WorldToHex(enemy.Get<RenderComponent>().Node3D.Position);

            // Move enemy toward player
            var path = _pathFinderSystem.FindPath(currentEnemyPos, playerPos);
            if (path.Count > 0)
            {
                await _animationSystem.MoveEntity(enemy, path);
                _turnSystem.EndTurn();
            }
        }

        public void GameOver()
        {
            GD.Print("Game Over - Player Died!");
        }

        public void Victory()
        {
            GD.Print("KILL EM ALL!");
        }
    }
}
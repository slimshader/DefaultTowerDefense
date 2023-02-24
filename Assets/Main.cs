using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace DefaultTowerDefense
{
    public readonly struct Prefab { }

    public struct Position
    {
        public float X, Y, Z;

        public Position(float x, float y, float z) => (X, Y, Z) = (x, y, z);

        public static Position operator +(Position left, Position right) => new Position(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    namespace transform
    {
        public struct Position2
        {
            public float X, Y;

            public Position2(float x, float y) => (X, Y) = (x, y);
        }
    }

    public struct Box
    {
        public readonly float Width, Height, Depth;

        public Box(float width, float height, float depth) => (Width, Height, Depth) = (width, height, depth);
    }

    public struct Direction
    {
        public int value;
    }

    public struct ChildOf<T> { }

    public struct Enemy { }

    public sealed class Health
    {
        public Health()
        {
            Value = 1;
        }

        public float Value = 1;
    }

    namespace Prefabs
    {
        public struct Path { }
        public struct Tile { }
        public struct Tree { }
        public struct Cannon { }
        public struct Laser { }
        public struct Enemy { }

    }
    public sealed class grid<T> where T : new()
    {

        public grid(int width, int height)
        {
            m_width = width;
            m_height = height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    m_values.Add(new T());
                }
            }
        }

        public void set(int x, int y, T value)
        {
            m_values[y * m_width + x] = value;
        }

        public T this[int x, int y]
        {
            get => m_values[y * m_width + x];
        }

        int m_width;
        int m_height;
        List<T> m_values = new();
    };

    public struct Game
    {
        public Entity window;
        public Entity level;

        public Position center;
        public float size;
    };


    public readonly struct Level
    {
        public Level(grid<bool> arg_map, transform.Position2 arg_spawn)
        {
            map = arg_map;
            spawn_point = arg_spawn;
        }

        public readonly grid<bool> map;
        public readonly transform.Position2 spawn_point;
    };

    // scopes

    public struct level { }
    public struct turrets { }

    public sealed class SpawnEnemySystem : AComponentSystem<float, Game>
    {
        private readonly float _interval;
        private float _timer;
        public SpawnEnemySystem(World world, float interval) : base(world)
        {
            _interval = interval;
        }

        protected override void Update(float state, ref Game component)
        {
            if (_timer >= _interval)
            {
                _timer = 0;

                var lvl = World.Get<Game>().level.Get<Level>();

                var e = World.CreateEntity();
                e.Set<Enemy>();
                e.Set(new Color(0.05f, 0.05f, 0.05f));
                e.Set(new Position(lvl.spawn_point.X, 1.2f, lvl.spawn_point.Y));
                e.Set(new Box(Main.EnemySize, Main.EnemySize, Main.EnemySize));
                e.Set(new Direction() { value = 0 });

            }

            _timer += state;
        }
    }

    public class Main : MonoBehaviour
    {
        public const float EnemySize = 0.7f;
        public const float EnemySpeed = 4.0f;
        public const float EnemySpawnInterval = 0.2f;

        const float TileSize = 3.0f;
        const float TileHeight = 0.5f;
        const float PathHeight = 0.1f;
        const float TileSpacing = 0.00f;
        const int TileCountX = 10;
        const int TileCountZ = 10;

        private World _world;

        private ISystem<float> _updateSystems;

        public Color TileColor;

        Entity TilePrefab => _world.GetEntities().With<Prefab>().With<Prefabs.Tile>().AsEnumerable().Single();

        void Start()
        {
            _world = new World();

            _world.SubscribeEntityComponentAdded((in Entity e, in Prefabs.Tile _) =>
            {
                if (e.Has<Prefab>())
                    return;

                var tilePrefab = TilePrefab;

                e.SetSameAs<Color>(tilePrefab);
                e.SetSameAs<Box>(tilePrefab);
            });

            _world.SubscribeEntityComponentAdded((in Entity e, in Prefabs.Path _) =>
            {
                e.Set(new Color(.2f, .2f, .2f));
                e.Set(new Box(TileSize + TileSpacing, PathHeight, TileSize + TileSpacing));
            });

            _world.SubscribeEntityComponentAdded((in Entity e, in Prefabs.Tree _) =>
            {
                var trunk = _world.CreateEntity();
                trunk.Set(e.Get<Position>() + new Position(0, .75f, 0));
                trunk.Set(new Color(.25f, .2f, .1f));
                trunk.Set(new Box(.5f, 1.5f, .5f));

                var canopy = _world.CreateEntity();
                canopy.Set(e.Get<Position>() + new Position(0, 2, 0));
                canopy.Set(new Color(.2f, .3f, .15f));
                canopy.Set(new Box(1.5f, 1.8f, 1.5f));
            });

            _updateSystems = new SequentialSystem<float>(
                new SpawnEnemySystem(_world, EnemySpawnInterval),
                new MoveEenemySystem(_world),

                new CreateBoxViewSystem(_world),
                new ViewPositionUpdateSystem(_world),
                new SetBoxViewColorSystem(_world)
                );

            InitPrefabs(_world);
            InitGame(_world);
            InitLevel(_world);

            TileColor = TilePrefab.Get<Color>();
        }

        // Update is called once per frame
        void Update()
        {
            TilePrefab.Set(TileColor);

            _updateSystems.Update(Time.deltaTime);
        }

        private static void InitGame(World ecs)
        {
            var g = new Game(); // ecs.Set<Game>();
            g.center = new(ToX(TileCountX / 2), 0, ToZ(TileCountZ / 2));
            g.size = TileCountX * (TileSize + TileSpacing) + 2;
            ecs.Set(g);
        }

        private static void InitPrefabs(World ecs)
        {
            {
                var e = ecs.CreateEntity();
                e.Set<Prefab>();
                e.Set<Prefabs.Tile>();
                e.Set(new Color(.2f, .34f, .15f));
                e.Set(new Box(TileSize, TileHeight, TileSize));
            }
        }

        private static void InitLevel(World ecs)
        {
            ref var g = ref ecs.Get<Game>();

            var path = new grid<bool>(TileCountX, TileCountZ);

            path.set(0, 1, true); path.set(1, 1, true); path.set(2, 1, true);
            path.set(3, 1, true); path.set(4, 1, true); path.set(5, 1, true);
            path.set(6, 1, true); path.set(7, 1, true); path.set(8, 1, true);
            path.set(8, 2, true); path.set(8, 3, true); path.set(7, 3, true);
            path.set(6, 3, true); path.set(5, 3, true); path.set(4, 3, true);
            path.set(3, 3, true); path.set(2, 3, true); path.set(1, 3, true);
            path.set(1, 4, true); path.set(1, 5, true); path.set(1, 6, true);
            path.set(1, 7, true); path.set(1, 8, true); path.set(2, 8, true);
            path.set(3, 8, true); path.set(4, 8, true); path.set(4, 7, true);
            path.set(4, 6, true); path.set(4, 5, true); path.set(5, 5, true);
            path.set(6, 5, true); path.set(7, 5, true); path.set(8, 5, true);
            path.set(8, 6, true); path.set(8, 7, true); path.set(7, 7, true);
            path.set(6, 7, true); path.set(6, 8, true); path.set(6, 9, true);
            path.set(7, 9, true); path.set(8, 9, true); path.set(9, 9, true);

            var spawnPoint = new transform.Position2(ToX(TileCountX - 1), ToZ(TileCountZ - 1));

            var level = ecs.CreateEntity();
            level.Set(new Level(path, spawnPoint));

            g.level = level;

            {
                var e = ecs.CreateEntity();
                e.Set(new Position(0, -2.5f, ToZ(TileCountZ / 2 - .5f)));
                e.Set(new Color(.11f, .15f, .1f));
                e.Set(new Box(ToX(TileCountX + .5f) * 2, 5, ToZ(TileCountZ + 2)));
            }

            for (int x = 0; x < TileCountX; x++)
            {
                for (int z = 0; z < TileCountZ; z++)
                {
                    var xc = ToX(x);
                    var zc = ToZ(z);

                    // level scope
                    var t = ecs.CreateEntity();
                    t.Set(new Position(xc, 0, zc));

                    if (path[x, z])
                    {
                        t.Set<Prefabs.Path>();
                    }
                    else
                    {
                        t.Set<Prefabs.Tile>();

                        var e = ecs.CreateEntity();
                        e.Set(new Position(xc, TileHeight / 2, zc));

                        if (Randf(1) > .65f)
                        {
                            e.Set<ChildOf<level>>();
                            e.Set<Prefabs.Tree>();
                        }
                        else
                        {
                            e.Set<ChildOf<turrets>>();
                            if (Randf(1) > 0.3f)
                            {
                                e.Set<Prefabs.Cannon>();
                            }
                            else
                            {
                                e.Set<Prefabs.Laser>();
                                // TODO e.Target
                            }
                        }
                    }

                }
            }
        }

        private static float Randf(float max) => UnityEngine.Random.Range(0, max);
        private static float to_coord(float x) => x * (TileSpacing + TileSize) - (TileSize / 2.0f);
        private static float ToX(float x) => to_coord(x + .5f) - to_coord(TileCountX / 2.0f);
        private static float ToZ(float z) => to_coord(z);

        public static float from_coord(float x)
        {
            return (x + (TileSize / 2.0f)) / (TileSpacing + TileSize);
        }

        public static float from_x(float x)
        {
            return from_coord(x + to_coord((TileCountX / 2.0f))) - 0.5f;
        }

        public static float from_z(float z)
        {
            return from_coord(z);
        }

        public static readonly transform.Position2[] dir = {
        new (-1, 0),
        new (0, -1),
        new (1, 0),
        new (0, 1),
        };

        // Check if enemy needs to change direction
        public static bool find_path(in Position p, ref Direction d, Level lvl)
        {
            // Check if enemy is in center of tile
            float t_x = from_x(p.X);
            float t_y = from_z(p.Z);
            int ti_x = (int)t_x;
            int ti_y = (int)t_y;
            float td_x = t_x - ti_x;
            float td_y = t_y - ti_y;

            // If enemy is in center of tile, decide where to go next
            if (td_x < 0.1 && td_y < 0.1)
            {
                grid<bool> tiles = lvl.map;

                // Compute backwards direction so we won't try to go there
                int backwards = (d.value + 2) % 4;

                // Find a direction that the enemy can move to
                for (int i = 0; i < 3; i++)
                {
                    int n_x = (int)(ti_x + dir[d.value].X);
                    int n_y = (int)(ti_y + dir[d.value].Y);

                    if (n_x >= 0 && n_x <= TileCountX)
                    {
                        if (n_y >= 0 && n_y <= TileCountZ)
                        {
                            // Next tile is still on the grid, test if it's a path
                            if (tiles[n_x, n_y])
                            {
                                // Next tile is a path, so continue along current direction
                                return false;
                            }
                        }
                    }

                    // Try next direction. Make sure not to move backwards
                    do
                    {
                        d.value = (d.value + 1) % 4;
                    } while (d.value == backwards);
                }

                // If enemy was not able to find a next direction, it reached the end
                return true;
            }

            return false;
        }

        public static void MoveEnemy(float delta_time, in Level lvl, in Entity entity, ref Position p, ref Direction d)
        {
            if (Main.find_path(p, ref d, lvl))
            {
                entity.Dispose();
            }
            else
            {
                p.X += dir[d.value].X * EnemySpeed * delta_time;
                p.Z += dir[d.value].Y * EnemySpeed * delta_time;
            }
        }
    }
}

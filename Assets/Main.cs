using DefaultEcs;
using DefaultEcs.System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultTowerDefense
{
    public readonly struct Position
    {
        public readonly float X, Y, Z;

        public Position(float x, float y, float z) => (X, Y, Z) = (x, y, z);
    }

    namespace transform
    {
        public readonly struct Position2
        {
            public readonly float X, Y;

            public Position2(float x, float y) => (X, Y) = (x, y);
        }
    }

    public struct Box
    {
        public readonly float Width, Height, Depth;

        public Box(float width, float height, float depth) => (Width, Height, Depth) = (width, height, depth);
    }

    public struct ChildOf<T> { }

    namespace Prefabs
    {
        public struct Path { }
        public struct Tile { }
        public struct Tree { }
        public struct Cannon { }
        public struct Laser { }

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

    struct Game
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

        readonly grid<bool> map;
        readonly transform.Position2 spawn_point;
    };

    // scopes

    public struct level { }
    public struct turrets { }

    public class Main : MonoBehaviour
    {

        const float TileSize = 3.0f;
        const float TileHeight = 0.5f;
        const float PathHeight = 0.1f;
        const float TileSpacing = 0.00f;
        const int TileCountX = 10;
        const int TileCountZ = 10;

        private World _world;

        private ISystem<float> _updateSystems;

        // Start is called before the first frame update
        void Start()
        {
            _world = new World();


            _world.SubscribeEntityComponentAdded((in Entity e, in Prefabs.Tile _) =>
            {
                e.Set(new Color(.2f, .34f, .15f));
                e.Set(new Box(TileSize, TileHeight, TileSize));
            });

            _world.SubscribeEntityComponentAdded((in Entity e, in Prefabs.Path _) =>
            {
                e.Set(new Color(.2f, .2f, .2f));
                e.Set(new Box(TileSize + TileSpacing, PathHeight, TileSize + TileSpacing));
            });

            _updateSystems = new CreateBoxViewSystem(_world);

            InitGame(_world);
            InitLevel(_world);
        }

        // Update is called once per frame
        void Update()
        {
            _updateSystems.Update(Time.deltaTime);
        }

        private static void InitGame(World ecs)
        {
            var g = new Game(); // ecs.Set<Game>();
            g.center = new(ToX(TileCountX / 2), 0, ToZ(TileCountZ / 2));
            g.size = TileCountX * (TileSize + TileSpacing) + 2;
            ecs.Set(g);
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
        private static float ToCoord(float x) => x * (TileSpacing + TileSize) - (TileSize / 2.0f);
        private static float ToX(float x) => ToCoord(x + .5f) - ToCoord(TileCountX / 2.0f);
        private static float ToZ(float z) => ToCoord(z);
    }
}

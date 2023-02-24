using DefaultEcs;
using DefaultEcs.System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultTowerDefense
{
    [Without(typeof(Prefab))]
    public sealed partial class CreateBoxViewSystem : AEntitySetSystem<float>
    {
        private List<GameObject> _objects = new();

        [Update, UseBuffer]
        private void Update(float _, in Entity entity, in Position position, [Added]in Box box)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Tile {_objects.Count}";

            //cube.GetComponent<Renderer>().material.color = color;
            cube.transform.localScale = new (box.Width, box.Height, box.Depth);        
            cube.transform.position = new Vector3(position.X, position.Y, position.Z);
            _objects.Add(cube);

            entity.Set(cube);
        }
    }

    //[With(typeof(Prefabs.Tile))]
    [Without(typeof(Prefab))]
    public sealed partial class SetBoxViewColorSystem : AEntitySetSystem<float>
    {
        [Update]
        private void Update(float _, in GameObject cube, in Color color)
        {
            cube.GetComponent<Renderer>().material.color = color;
        }
    }

    [With(typeof(Enemy))]
    public sealed partial class MoveEenemySystem : AEntitySetSystem<float>
    {
        [Update, UseBuffer]
        private void Update(float dt, in Entity entity, ref Position p, ref Direction direction)
        {
            var lvl = World.Get<Game>().level.Get<Level>();

            Main.MoveEnemy(dt, lvl, entity, ref p, ref direction);
        }


    }

    public sealed partial class ViewPositionUpdateSystem : AEntitySetSystem<float>
    {
        [Update]
        private void Update(float _, in GameObject cube, in Position position)
        {
            cube.transform.position = new (position.X, position.Y, position.Z);
        }
    }

}

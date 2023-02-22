using DefaultEcs.System;
using UnityEngine;

namespace DefaultTowerDefense
{
    public sealed partial class CreateBoxViewSystem : AEntitySetSystem<float>
    {
        [Update]
        private void Update(float _, in Position position, [Added]in Box box, in Color color)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.GetComponent<Renderer>().material.color = color;
            cube.transform.localScale = new (box.Width, box.Height, box.Depth);
            cube.transform.position = new Vector3(position.X, position.Y, position.Z);
        }
    }
}

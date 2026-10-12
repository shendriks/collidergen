using System.Numerics;
using System.Text.Json;
using ColliderGen.Cli;
using NUnit.Framework;

namespace ColliderGen.Tests;

[TestFixture]
[TestOf(typeof(ColliderDto))]
public class ColliderDtoTests
{
    [Test]
    public void Serialize_ProducesFramesOfVerticesArraysOfXyObjects()
    {
        var dto = new ColliderDto([
            new FrameDto([new VertexDto(-18f, -18f), new VertexDto(18f, -18f)]),
            new FrameDto([new VertexDto(0f, 1f)])
        ]);

        var json = JsonSerializer.Serialize(dto);

        Assert.That(json, Is.EqualTo("{\"Frames\":[{\"Vertices\":[{\"X\":-18,\"Y\":-18},{\"X\":18,\"Y\":-18}]},{\"Vertices\":[{\"X\":0,\"Y\":1}]}]}"));
    }

    [Test]
    public void From_Collider_MapsEachFrameVertexToXy()
    {
        var collider = new SpriteCollider(2, Vector2.Zero, [[new Vector2(1, 2), new Vector2(3, 4)], []]);

        var dto = ColliderDto.From(collider);

        Assert.That(dto.Frames, Has.Length.EqualTo(2));
        Assert.That(dto.Frames[0].Vertices, Is.EqualTo([new VertexDto(1, 2), new VertexDto(3, 4)]));
        Assert.That(dto.Frames[1].Vertices, Is.Empty);
    }

    [Test]
    public void Serialize_EmptyFrame_ProducesEmptyVerticesArray()
    {
        var json = JsonSerializer.Serialize(new ColliderDto([new FrameDto([])]));

        Assert.That(json, Is.EqualTo("{\"Frames\":[{\"Vertices\":[]}]}"));
    }
}

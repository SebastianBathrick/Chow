using Chow.Repl;

namespace Chow.Tests.UnitTests;

public class CursorTests
{
    [Test]
    public void New_Cursor_StartsAtOrigin()
    {
        var cursor = new Cursor();

        Assert.Multiple(() =>
        {
            Assert.That(cursor.X, Is.EqualTo(0));
            Assert.That(cursor.Y, Is.EqualTo(0));
        });
    }

    [Test]
    public void MoveUp_FromOrigin_IncrementsYAndReturnsIt()
    {
        var cursor = new Cursor();

        var result = cursor.MoveUp();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(1));
            Assert.That(cursor.Y, Is.EqualTo(1));
        });
    }

    [Test]
    public void MoveDown_AboveFirstLine_DecrementsYAndReturnsIt()
    {
        var cursor = new Cursor();
        cursor.MoveUp();
        cursor.MoveUp();

        var result = cursor.MoveDown();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(1));
            Assert.That(cursor.Y, Is.EqualTo(1));
        });
    }

    [Test]
    public void MoveDown_AtFirstLine_ClampsAtZeroAndReturnsZero()
    {
        var cursor = new Cursor();

        var result = cursor.MoveDown();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(cursor.Y, Is.EqualTo(0));
        });
    }

    [Test]
    public void MoveRight_FromOrigin_IncrementsXAndReturnsIt()
    {
        var cursor = new Cursor();

        var result = cursor.MoveRight();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(1));
            Assert.That(cursor.X, Is.EqualTo(1));
        });
    }

    [Test]
    public void MoveLeft_AfterMoveRight_DecrementsXAndReturnsIt()
    {
        var cursor = new Cursor();
        cursor.MoveRight();
        cursor.MoveRight();

        var result = cursor.MoveLeft();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(1));
            Assert.That(cursor.X, Is.EqualTo(1));
        });
    }

    [Test]
    public void MoveLeft_AtFirstColumn_ClampsAtZeroAndReturnsZero()
    {
        var cursor = new Cursor();

        var result = cursor.MoveLeft();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(cursor.X, Is.EqualTo(0));
        });
    }

    [Test]
    public void JumpToColumn_PositiveIndex_MovesXToIndex()
    {
        var cursor = new Cursor();

        cursor.JumpToColumn(7);

        Assert.That(cursor.X, Is.EqualTo(7));
    }

    [Test]
    public void JumpToColumn_NegativeIndex_ClampsToZero()
    {
        var cursor = new Cursor();
        cursor.JumpToColumn(5);

        cursor.JumpToColumn(-3);

        Assert.That(cursor.X, Is.EqualTo(0));
    }

    [Test]
    public void JumpToFirstColumn_FromColumn_ZeroesXLeavesYAndReturnsZero()
    {
        var cursor = new Cursor();
        cursor.JumpToColumn(4);
        cursor.MoveUp();

        var result = cursor.JumpToFirstColumn();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(cursor.X, Is.EqualTo(0));
            Assert.That(cursor.Y, Is.EqualTo(1));
        });
    }

    [Test]
    public void JumpFirstLine_FromLine_ZeroesYLeavesXAndReturnsZero()
    {
        var cursor = new Cursor();
        cursor.MoveUp();
        cursor.MoveUp();
        cursor.MoveRight();

        var result = cursor.JumpFirstLine();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(cursor.Y, Is.EqualTo(0));
            Assert.That(cursor.X, Is.EqualTo(1));
        });
    }

    [Test]
    public void JumpToStart_FromAnyPosition_ZeroesBothAxes()
    {
        var cursor = new Cursor();
        cursor.MoveUp();
        cursor.MoveRight();
        cursor.MoveRight();

        cursor.JumpToStart();

        Assert.Multiple(() =>
        {
            Assert.That(cursor.X, Is.EqualTo(0));
            Assert.That(cursor.Y, Is.EqualTo(0));
        });
    }
}

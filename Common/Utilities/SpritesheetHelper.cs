using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WorldShapingWandsMod.Common.Utilities;

/// <summary>
/// Common helper for grid-based spritesheet animation. Converts a flat texture
/// into column × row addressing, with optional continuous rotation.
/// </summary>
/// <remarks>
/// <para>
/// Use this whenever a sprite texture is laid out as a uniform grid (e.g. 4×4).
/// Instead of manually computing <c>sourceRect = new Rectangle(col * w, row * h, w, h)</c>
/// everywhere, callers get a clean API with named parameters.
/// </para>
/// <para>
/// Also provides a timer-based frame advancer for looping animations,
/// so animation constants (frame count, frame rate) stay in one place.
/// </para>
/// </remarks>
public sealed class SpritesheetHelper
{
    // ================================================================
    //  Grid Layout
    // ================================================================

    /// <summary>Number of columns in the spritesheet grid.</summary>
    public int Columns { get; }

    /// <summary>Number of rows in the spritesheet grid.</summary>
    public int Rows { get; }

    /// <summary>Width of a single frame in pixels.</summary>
    public int FrameWidth { get; private set; }

    /// <summary>Height of a single frame in pixels.</summary>
    public int FrameHeight { get; private set; }

    // ================================================================
    //  Animation State
    // ================================================================

    /// <summary>Current column (X) in the grid, 0-based.</summary>
    public int CurrentColumn { get; set; }

    /// <summary>Current row (Y) in the grid, 0-based.</summary>
    public int CurrentRow { get; set; }

    /// <summary>Internal tick counter for frame advancement.</summary>
    private int _animTimer;

    // ================================================================
    //  Constructor
    // ================================================================

    /// <summary>
    /// Creates a spritesheet helper for a grid of <paramref name="columns"/> × <paramref name="rows"/>.
    /// Frame dimensions are computed lazily from the first texture passed to <see cref="GetSourceRect"/>,
    /// or can be set explicitly via <see cref="SetFrameSize"/>.
    /// </summary>
    public SpritesheetHelper(int columns, int rows)
    {
        Columns = columns;
        Rows = rows;
    }

    /// <summary>
    /// Explicitly sets the frame size. Useful when the texture is not yet loaded
    /// or when the grid doesn't evenly divide the texture.
    /// </summary>
    public void SetFrameSize(int width, int height)
    {
        FrameWidth = width;
        FrameHeight = height;
    }

    // ================================================================
    //  Source Rectangle
    // ================================================================

    /// <summary>
    /// Returns the source rectangle for the cell at (<paramref name="column"/>, <paramref name="row"/>).
    /// Computes frame size from <paramref name="texture"/> if not already set.
    /// </summary>
    public Rectangle GetSourceRect(Texture2D texture, int column, int row)
    {
        EnsureFrameSize(texture);
        return new Rectangle(
            column * FrameWidth,
            row * FrameHeight,
            FrameWidth,
            FrameHeight);
    }

    /// <summary>
    /// Returns the source rectangle for the current animation cell
    /// (<see cref="CurrentColumn"/>, <see cref="CurrentRow"/>).
    /// </summary>
    public Rectangle GetCurrentSourceRect(Texture2D texture)
    {
        return GetSourceRect(texture, CurrentColumn, CurrentRow);
    }

    /// <summary>
    /// Returns the origin point at the center of a single frame.
    /// </summary>
    public Vector2 GetFrameOrigin(Texture2D texture)
    {
        EnsureFrameSize(texture);
        return new Vector2(FrameWidth / 2f, FrameHeight / 2f);
    }

    // ================================================================
    //  Animation
    // ================================================================

    /// <summary>
    /// Advances the column animation by one tick. When <paramref name="ticksPerFrame"/>
    /// ticks have elapsed, <see cref="CurrentColumn"/> advances by 1 and wraps.
    /// Call once per frame in AI() or similar.
    /// </summary>
    /// <param name="ticksPerFrame">Number of game ticks between column advances.</param>
    /// <returns><c>true</c> if the column just wrapped around to 0.</returns>
    public bool AdvanceColumn(int ticksPerFrame)
    {
        _animTimer++;
        if (_animTimer >= ticksPerFrame)
        {
            _animTimer = 0;
            CurrentColumn = (CurrentColumn + 1) % Columns;
            return CurrentColumn == 0;
        }
        return false;
    }

    /// <summary>
    /// Resets the animation timer and frame position.
    /// </summary>
    public void Reset()
    {
        _animTimer = 0;
        CurrentColumn = 0;
        CurrentRow = 0;
        _continuousRotation = 0f;
    }

    // ================================================================
    //  Continuous Rotation (Single-Column Mode)
    // ================================================================

    /// <summary>Current continuous rotation angle in radians.</summary>
    private float _continuousRotation;

    /// <summary>
    /// Gets the current continuous rotation angle in radians.
    /// Use this instead of discrete column frames when the sprite
    /// has only one visual frame and should rotate smoothly.
    /// </summary>
    public float ContinuousRotation => _continuousRotation;

    /// <summary>
    /// Advances the continuous rotation angle by a fixed increment per tick.
    /// Useful when the sprite has a single column and should schoice via rotation
    /// rather than discrete frame animation.
    /// </summary>
    /// <param name="radiansPerTick">Rotation speed in radians per game tick.
    /// At 60 FPS, a value of <c>MathHelper.TwoPi / 60</c> gives one full rotation per second.</param>
    public void AdvanceRotation(float radiansPerTick)
    {
        _continuousRotation += radiansPerTick;

        // Wrap to [0, 2π) to prevent floating-point drift over long lifetimes
        if (_continuousRotation >= MathHelper.TwoPi)
            _continuousRotation -= MathHelper.TwoPi;
    }

    // ================================================================
    //  Private Helpers
    // ================================================================

    private void EnsureFrameSize(Texture2D texture)
    {
        if (FrameWidth > 0 && FrameHeight > 0)
            return;

        FrameWidth = texture.Width / Columns;
        FrameHeight = texture.Height / Rows;
    }
}

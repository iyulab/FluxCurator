using FluxCurator.Core.Domain;

namespace FluxCurator.Core;

/// <summary>
/// Interface for text refinement operations.
/// Text refinement cleans and normalizes raw text before further processing.
/// </summary>
public interface ITextRefiner
{
    /// <summary>
    /// Refines text by removing noise and normalizing content.
    /// </summary>
    /// <param name="text">The text to refine.</param>
    /// <param name="options">Refinement options.</param>
    /// <returns>The refined text.</returns>
    string Refine(string text, TextRefineOptions options);

    /// <summary>
    /// Refines text using default options (Light).
    /// </summary>
    /// <param name="text">The text to refine.</param>
    /// <returns>The refined text.</returns>
    string Refine(string text);
}

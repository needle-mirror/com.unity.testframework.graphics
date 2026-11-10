namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// A version of the per-pixel deltaE, the JzAzBz color difference of Safdar et al. 2017. Reference
    /// images and thresholds are made with one version, so a comparison must keep measuring with it. A
    /// member's value must never change.
    /// </summary>
    internal enum DeltaEFormula
    {
        /// <summary>
        /// The formula reference images have been made with. Its hue angle covers half the circle, so
        /// colors whose red-green component changes sign, such as near-neutral grays, score too high and
        /// opposite hues too low, and its LMS matrix has 0.41478372 where the paper gives 0.41478972.
        /// </summary>
        Original = 0,

        /// <summary>
        /// The formula as Safdar et al. 2017 define it: a full-circle hue angle and the paper's LMS matrix.
        /// </summary>
        Corrected = 1,
    }
}

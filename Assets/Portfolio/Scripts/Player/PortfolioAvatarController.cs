using System;
using UnityEngine;
using PortfolioInputManager = Portfolio.Input.InputManager;

namespace Portfolio.Player
{
    [Obsolete("Use PlayerController. This class remains as a compatibility alias for older prototype scenes.")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PortfolioInputManager))]
    public sealed class PortfolioAvatarController : PlayerController
    {
    }
}

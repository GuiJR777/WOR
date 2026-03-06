// Purpose: Defines presenter lifecycle for binding model and view.
namespace WOR.Gameplay.Core.Mvp {

    public interface IPresenter {

        bool IsBound { get; }
        void Bind();
        void Unbind();
    }
}


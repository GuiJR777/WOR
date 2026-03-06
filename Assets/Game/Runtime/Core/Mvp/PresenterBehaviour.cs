// Purpose: Provides a reusable MonoBehaviour presenter base with safe bind/unbind lifecycle.
using UnityEngine;

namespace WOR.Gameplay.Core.Mvp {

    public abstract class PresenterBehaviour<TView, TModel> : MonoBehaviour, IPresenter
        where TView : class, IView
        where TModel : class, IModel {

        protected TView View { get; private set; }
        protected TModel Model { get; private set; }
        public bool IsBound { get; private set; }

        protected virtual void Awake() {
            View = ResolveView();
            Model = CreateModel();
        }

        protected virtual void OnEnable() {
            Bind();
        }

        protected virtual void OnDisable() {
            Unbind();
        }

        public void Bind() {
            if(IsBound) {
                return;
            }

            if(View == null) {
                View = ResolveView();
            }
            if(Model == null) {
                Model = CreateModel();
            }

            if(View == null || Model == null) {
                Debug.LogError($"{GetType().Name} failed to bind because model or view is null.", this);
                return;
            }

            OnBind(Model, View);
            IsBound = true;
        }

        public void Unbind() {
            if(!IsBound) {
                return;
            }

            OnUnbind();
            IsBound = false;
        }

        protected abstract TView ResolveView();
        protected abstract TModel CreateModel();
        protected abstract void OnBind(TModel model, TView view);
        protected virtual void OnUnbind() {
        }
    }
}



# CounterDrone (Unity ML-Agents) — Cloud Build + Colab

This repo contains the scripts and training config for a counter‑drone interception demo using Unity ML‑Agents.

## What this gives you
- `Assets/Scripts/*.cs`: Defender agent, Target drone, Game manager, and UI helpers.
- `interceptor_pro.yaml`: PPO trainer config with curriculum.
- A Colab notebook is included at `colab_training.ipynb` for training in the cloud.

## Cloud-only workflow (no local Unity)
1. Create a new GitHub repo and upload this folder.
2. Open **Unity Dashboard → Build Automation (Cloud Build)**.
3. Connect your GitHub repo, create a **Linux Server** build target, and **Build**.
4. Download the Linux headless artifact (`.x86_64` + `_Data`).
5. Open **Google Colab**, upload the build zip, unzip to `/content/env/`.
6. Install ML-Agents and run training:

```
!pip install mlagents==0.30.0 torch>=2.1 tensorboard
!mlagents-learn /content/interceptor_pro.yaml --run-id=intercept-pro --no-graphics --env=/content/env/CounterDrone.x86_64
```

7. Trained model (`.onnx`) will appear under `/content/results/.../DroneInterceptor.onnx`. Download it.
8. Commit the `.onnx` to `Assets/Models/` in your repo, assign it to the Agent's **Behavior Parameters → Model**, and trigger another Cloud Build for a demo executable.

## Notes
- Tweak difficulty via `DroneGameManager.initialCaptureRadius` / `minCaptureRadius` and environment randomization in `TargetDrone`.
- For faster training, increase `engine_settings.num_envs` if GPU allows.


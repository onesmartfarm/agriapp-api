import { Router, type IRouter } from "express";
import { authenticate, authorize } from "../middlewares/jwtMiddleware";
import { UserRepository } from "../infrastructure/repositories/userRepository";
import { Role } from "../core/enums";

const router: IRouter = Router();
const userRepo = new UserRepository();

router.get(
  "/users",
  authenticate,
  authorize(Role.SuperUser, Role.Manager),
  async (req, res) => {
    try {
      const users = await userRepo.findAll();
      const sanitized = users.map(({ passwordHash, ...rest }) => rest);
      res.json(sanitized);
    } catch (err) {
      req.log.error({ err }, "Failed to fetch users");
      res.status(500).json({ error: "Internal server error" });
    }
  },
);

router.get("/users/me", authenticate, async (req, res) => {
  try {
    const user = await userRepo.findById(req.currentUser!.userId);
    if (!user) {
      res.status(404).json({ error: "User not found" });
      return;
    }
    const { passwordHash, ...sanitized } = user;
    res.json(sanitized);
  } catch (err) {
    req.log.error({ err }, "Failed to fetch current user");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;

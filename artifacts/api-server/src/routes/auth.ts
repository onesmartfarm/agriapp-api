import { Router, type IRouter } from "express";
import bcrypt from "bcryptjs";
import { UserRepository } from "../infrastructure/repositories/userRepository";
import { generateToken } from "../middlewares/jwtMiddleware";
import type { Role } from "../core/enums";

const router: IRouter = Router();
const userRepo = new UserRepository();

router.post("/auth/login", async (req, res) => {
  try {
    const { email, password } = req.body;

    if (!email || !password) {
      res.status(400).json({ error: "Email and password are required" });
      return;
    }

    const user = await userRepo.findByEmail(email);
    if (!user) {
      res.status(401).json({ error: "Invalid credentials" });
      return;
    }

    const validPassword = await bcrypt.compare(password, user.passwordHash);
    if (!validPassword) {
      res.status(401).json({ error: "Invalid credentials" });
      return;
    }

    const token = generateToken({
      userId: user.id,
      email: user.email,
      role: user.role as Role,
      centerId: user.centerId,
    });

    res.json({
      token,
      user: {
        id: user.id,
        name: user.name,
        email: user.email,
        role: user.role,
        centerId: user.centerId,
      },
    });
  } catch (err) {
    req.log.error({ err }, "Login failed");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.post("/auth/register", async (req, res) => {
  try {
    const { name, email, password, role, centerId } = req.body;

    if (!name || !email || !password) {
      res.status(400).json({ error: "Name, email, and password are required" });
      return;
    }

    const existing = await userRepo.findByEmail(email);
    if (existing) {
      res.status(409).json({ error: "Email already registered" });
      return;
    }

    const passwordHash = await bcrypt.hash(password, 10);
    const user = await userRepo.create({
      name,
      email,
      passwordHash,
      role: role || "Staff",
      centerId: centerId || null,
    });

    const token = generateToken({
      userId: user.id,
      email: user.email,
      role: user.role as Role,
      centerId: user.centerId,
    });

    res.status(201).json({
      token,
      user: {
        id: user.id,
        name: user.name,
        email: user.email,
        role: user.role,
        centerId: user.centerId,
      },
    });
  } catch (err) {
    req.log.error({ err }, "Registration failed");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;

import { Request, Response, NextFunction } from "express";
import jwt from "jsonwebtoken";
import type { ICurrentUser } from "../core/interfaces";
import { Role } from "../core/enums";

const JWT_SECRET = process.env.SESSION_SECRET || "agri-rental-secret-key";

declare global {
  namespace Express {
    interface Request {
      currentUser?: ICurrentUser;
    }
  }
}

export function generateToken(user: ICurrentUser): string {
  return jwt.sign(
    {
      userId: user.userId,
      email: user.email,
      role: user.role,
      centerId: user.centerId,
    },
    JWT_SECRET,
    { expiresIn: "24h" },
  );
}

export function authenticate(req: Request, res: Response, next: NextFunction): void {
  const authHeader = req.headers.authorization;

  if (!authHeader || !authHeader.startsWith("Bearer ")) {
    res.status(401).json({ error: "Authentication required" });
    return;
  }

  const token = authHeader.substring(7);

  try {
    const decoded = jwt.verify(token, JWT_SECRET) as any;
    req.currentUser = {
      userId: decoded.userId,
      email: decoded.email,
      role: decoded.role as Role,
      centerId: decoded.centerId,
    };
    next();
  } catch {
    res.status(401).json({ error: "Invalid or expired token" });
  }
}

export function authorize(...roles: Role[]) {
  return (req: Request, res: Response, next: NextFunction): void => {
    if (!req.currentUser) {
      res.status(401).json({ error: "Authentication required" });
      return;
    }

    if (roles.length > 0 && !roles.includes(req.currentUser.role)) {
      res.status(403).json({ error: "Insufficient permissions" });
      return;
    }

    next();
  };
}

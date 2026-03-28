import { Router, type IRouter } from "express";
import { authenticate, authorize } from "../middlewares/jwtMiddleware";
import { EquipmentRepository } from "../infrastructure/repositories/equipmentRepository";
import { calculateRentalWithGst } from "../application/services/gstCalculator";
import { calculateCommission } from "../application/services/commissionRules";
import { Role } from "../core/enums";

const router: IRouter = Router();
const equipmentRepo = new EquipmentRepository();

router.get("/equipment", authenticate, async (req, res) => {
  try {
    const equipment = await equipmentRepo.findAll(req.currentUser!);
    res.json(equipment);
  } catch (err) {
    req.log.error({ err }, "Failed to fetch equipment");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/equipment/:id", authenticate, async (req, res) => {
  try {
    const id = parseInt(req.params.id);
    const equipment = await equipmentRepo.findById(id, req.currentUser!);
    if (!equipment) {
      res.status(404).json({ error: "Equipment not found" });
      return;
    }
    res.json(equipment);
  } catch (err) {
    req.log.error({ err }, "Failed to fetch equipment");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.post(
  "/equipment",
  authenticate,
  authorize(Role.SuperUser, Role.Manager),
  async (req, res) => {
    try {
      const { name, category, hourlyRate, centerId } = req.body;
      const equipment = await equipmentRepo.create({
        name,
        category,
        hourlyRate: String(hourlyRate),
        centerId,
      });
      res.status(201).json(equipment);
    } catch (err) {
      req.log.error({ err }, "Failed to create equipment");
      res.status(500).json({ error: "Internal server error" });
    }
  },
);

router.put(
  "/equipment/:id",
  authenticate,
  authorize(Role.SuperUser, Role.Manager),
  async (req, res) => {
    try {
      const id = parseInt(req.params.id);
      const updated = await equipmentRepo.update(id, req.body, req.currentUser!);
      if (!updated) {
        res.status(404).json({ error: "Equipment not found" });
        return;
      }
      res.json(updated);
    } catch (err) {
      req.log.error({ err }, "Failed to update equipment");
      res.status(500).json({ error: "Internal server error" });
    }
  },
);

router.delete(
  "/equipment/:id",
  authenticate,
  authorize(Role.SuperUser, Role.Manager),
  async (req, res) => {
    try {
      const id = parseInt(req.params.id);
      const deleted = await equipmentRepo.delete(id, req.currentUser!);
      if (!deleted) {
        res.status(404).json({ error: "Equipment not found" });
        return;
      }
      res.json({ message: "Equipment deleted" });
    } catch (err) {
      req.log.error({ err }, "Failed to delete equipment");
      res.status(500).json({ error: "Internal server error" });
    }
  },
);

router.post("/equipment/:id/quote", authenticate, async (req, res) => {
  try {
    const id = parseInt(req.params.id);
    const { hours } = req.body;

    if (!hours || hours <= 0) {
      res.status(400).json({ error: "Valid hours required" });
      return;
    }

    const equipment = await equipmentRepo.findById(id, req.currentUser!);
    if (!equipment) {
      res.status(404).json({ error: "Equipment not found" });
      return;
    }

    const hourlyRate = parseFloat(equipment.hourlyRate);
    const gstBreakdown = calculateRentalWithGst(hourlyRate, hours);
    const commission = calculateCommission(gstBreakdown.baseAmount);

    res.json({
      equipment: { id: equipment.id, name: equipment.name, category: equipment.category },
      hours,
      pricing: gstBreakdown,
      commission,
    });
  } catch (err) {
    req.log.error({ err }, "Failed to generate quote");
    res.status(500).json({ error: "Internal server error" });
  }
});

export default router;

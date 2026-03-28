import { Router, type IRouter } from "express";
import { authenticate, authorize } from "../middlewares/jwtMiddleware";
import { WorkOrderRepository } from "../infrastructure/repositories/workOrderRepository";
import { Role } from "../core/enums";

const router: IRouter = Router();
const workOrderRepo = new WorkOrderRepository();

router.get("/work-orders", authenticate, async (req, res) => {
  try {
    const workOrders = await workOrderRepo.findAll(req.currentUser!);
    res.json(workOrders);
  } catch (err) {
    req.log.error({ err }, "Failed to fetch work orders");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/work-orders/:id", authenticate, async (req, res) => {
  try {
    const id = parseInt(req.params.id);
    const workOrder = await workOrderRepo.findById(id, req.currentUser!);
    if (!workOrder) {
      res.status(404).json({ error: "Work order not found" });
      return;
    }
    res.json(workOrder);
  } catch (err) {
    req.log.error({ err }, "Failed to fetch work order");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.post(
  "/work-orders",
  authenticate,
  authorize(Role.SuperUser, Role.Manager, Role.Supervisor),
  async (req, res) => {
    try {
      const { equipmentId, staffId, description, centerId } = req.body;
      const workOrder = await workOrderRepo.create({
        equipmentId,
        staffId,
        description,
        centerId: centerId || req.currentUser!.centerId!,
        status: "Pending",
      });
      res.status(201).json(workOrder);
    } catch (err) {
      req.log.error({ err }, "Failed to create work order");
      res.status(500).json({ error: "Internal server error" });
    }
  },
);

router.patch(
  "/work-orders/:id/status",
  authenticate,
  authorize(Role.SuperUser, Role.Manager, Role.Supervisor, Role.Staff),
  async (req, res) => {
    try {
      const id = parseInt(req.params.id);
      const { status } = req.body;

      if (!status) {
        res.status(400).json({ error: "Status is required" });
        return;
      }

      const updated = await workOrderRepo.updateStatus(id, status, req.currentUser!);
      if (!updated) {
        res.status(404).json({ error: "Work order not found or access denied" });
        return;
      }
      res.json(updated);
    } catch (err) {
      req.log.error({ err }, "Failed to update work order status");
      res.status(500).json({ error: "Internal server error" });
    }
  },
);

export default router;

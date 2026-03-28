import { Router, type IRouter } from "express";
import { authenticate, authorize } from "../middlewares/jwtMiddleware";
import { InquiryRepository } from "../infrastructure/repositories/inquiryRepository";
import { Role } from "../core/enums";

const router: IRouter = Router();
const inquiryRepo = new InquiryRepository();

router.get("/inquiries", authenticate, async (req, res) => {
  try {
    const inquiries = await inquiryRepo.findAll(req.currentUser!);
    res.json(inquiries);
  } catch (err) {
    req.log.error({ err }, "Failed to fetch inquiries");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.get("/inquiries/:id", authenticate, async (req, res) => {
  try {
    const id = parseInt(req.params.id);
    const inquiry = await inquiryRepo.findById(id, req.currentUser!);
    if (!inquiry) {
      res.status(404).json({ error: "Inquiry not found" });
      return;
    }
    res.json(inquiry);
  } catch (err) {
    req.log.error({ err }, "Failed to fetch inquiry");
    res.status(500).json({ error: "Internal server error" });
  }
});

router.post(
  "/inquiries",
  authenticate,
  authorize(Role.SuperUser, Role.Manager, Role.Sales),
  async (req, res) => {
    try {
      const { customerId, equipmentId, salespersonId, centerId } = req.body;

      if (req.currentUser!.role === Role.Sales && salespersonId !== req.currentUser!.userId) {
        res.status(403).json({ error: "Sales users can only create inquiries for themselves" });
        return;
      }

      const inquiry = await inquiryRepo.create({
        customerId,
        equipmentId,
        salespersonId,
        centerId: centerId || req.currentUser!.centerId!,
        status: "New",
      });
      res.status(201).json(inquiry);
    } catch (err) {
      req.log.error({ err }, "Failed to create inquiry");
      res.status(500).json({ error: "Internal server error" });
    }
  },
);

router.patch(
  "/inquiries/:id/status",
  authenticate,
  authorize(Role.SuperUser, Role.Manager, Role.Sales),
  async (req, res) => {
    try {
      const id = parseInt(req.params.id);
      const { status } = req.body;

      if (!status) {
        res.status(400).json({ error: "Status is required" });
        return;
      }

      const updated = await inquiryRepo.updateStatus(id, status, req.currentUser!);
      if (!updated) {
        res.status(404).json({ error: "Inquiry not found or access denied" });
        return;
      }
      res.json(updated);
    } catch (err) {
      req.log.error({ err }, "Failed to update inquiry status");
      res.status(500).json({ error: "Internal server error" });
    }
  },
);

export default router;

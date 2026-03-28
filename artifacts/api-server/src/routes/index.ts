import { Router, type IRouter } from "express";
import healthRouter from "./health";
import authRouter from "./auth";
import equipmentRouter from "./equipment";
import inquiriesRouter from "./inquiries";
import workOrdersRouter from "./workOrders";
import usersRouter from "./users";

const router: IRouter = Router();

router.use(healthRouter);
router.use(authRouter);
router.use(equipmentRouter);
router.use(inquiriesRouter);
router.use(workOrdersRouter);
router.use(usersRouter);

export default router;
